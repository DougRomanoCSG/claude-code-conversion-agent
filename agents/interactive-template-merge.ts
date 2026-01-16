#!/usr/bin/env bun
/**
 * INTERACTIVE TEMPLATE MERGE AGENT
 *
 * Intelligently merges generated code templates with existing implementations,
 * preserving custom logic while incorporating new features.
 *
 * Usage:
 *   bun run agents/interactive-template-merge.ts --entity "Customer"
 *   bun run agents/interactive-template-merge.ts --entity "Customer" --dry-run
 *   bun run agents/interactive-template-merge.ts --entity "Customer" --auto
 *   bun run agents/interactive-template-merge.ts --rollback --entity "Customer"
 */

import { readFileSync, existsSync, writeFileSync, copyFileSync, mkdirSync, unlinkSync } from 'fs';
import { join, dirname } from 'path';
import { getProjectRoot, getAdminApiPath, getAdminUiPath, getSharedProjectPath } from '../lib/paths';
import prompts from 'prompts';
import { diffLines } from 'diff';

const projectRoot = getProjectRoot(import.meta.url);

// ============================================================================
// Types and Interfaces
// ============================================================================

interface ParsedMethod {
  name: string;
  signature: string;
  startLine: number;
  endLine: number;
  fullText: string;
  attributes: string[];
  isPublic: boolean;
  isAsync: boolean;
  returnType: string;
  parameters: string;
}

interface ParsedClass {
  name: string;
  methods: ParsedMethod[];
  properties: ParsedProperty[];
  usings: string[];
  namespace: string;
  rawContent: string;
}

interface ParsedProperty {
  name: string;
  type: string;
  attributes: string[];
  accessibility: string;
  isReadOnly: boolean;
  hasGetter: boolean;
  hasSetter: boolean;
  fullText: string;
  startLine: number;
}

interface ParsedRazorSection {
  name: string;
  startLine: number;
  endLine: number;
  fullText: string;
}

interface ParsedRazorView {
  modelDeclaration: string;
  sections: ParsedRazorSection[];
  rawContent: string;
}

interface MergeAnalysis {
  newMethods: ParsedMethod[];
  changedMethods: { generated: ParsedMethod; existing: ParsedMethod }[];
  removedMethods: ParsedMethod[];
  unchangedMethods: ParsedMethod[];
  newProperties: ParsedProperty[];
  changedProperties: { generated: ParsedProperty; existing: ParsedProperty }[];
  conflicts: string[];
}

interface MergeOptions {
  entity: string;
  templatePath?: string;
  targetPath?: string;
  mode: 'interactive' | 'auto' | 'dry-run';
  conflictStrategy: 'prompt' | 'keep-existing' | 'use-generated';
  fileTypes: ('cs' | 'cshtml' | 'sql')[];
  rollback: boolean;
}

interface FileMergeResult {
  filePath: string;
  status: 'merged' | 'skipped' | 'conflict' | 'error' | 'copied';
  methodsAdded: number;
  methodsChanged: number;
  methodsPreserved: number;
  propertiesAdded: number;
  conflicts: string[];
  error?: string;
}

interface FileOperation {
  generated: string;
  existing: string;
  type: 'ui' | 'api' | 'shared';
  exists: boolean;
}

// ============================================================================
// C# Parsing Functions (Refined from POC)
// ============================================================================

/**
 * Parse C# file to extract class structure
 */
function parseCSharpFile(filePath: string): ParsedClass | null {
  if (!existsSync(filePath)) {
    return null;
  }

  const content = readFileSync(filePath, 'utf-8');
  const lines = content.split('\n');

  // Extract namespace
  const namespaceMatch = content.match(/namespace\s+([\w.]+)/);
  const namespace = namespaceMatch ? namespaceMatch[1] : '';

  // Extract using statements
  const usings: string[] = [];
  const usingRegex = /^using\s+([\w.]+);/gm;
  let match;
  while ((match = usingRegex.exec(content)) !== null) {
    usings.push(match[1]);
  }

  // Extract class name
  const classMatch = content.match(/(?:public|internal|private)?\s*(?:partial)?\s*class\s+(\w+)/);
  const className = classMatch ? classMatch[1] : '';

  // Extract methods (refined regex)
  const methods = extractMethods(content, lines);

  // Extract properties
  const properties = extractProperties(content, lines);

  return {
    name: className,
    methods,
    properties,
    usings,
    namespace,
    rawContent: content,
  };
}

/**
 * Extract all method definitions from C# content
 * REFINED: Better filtering to avoid false positives
 */
function extractMethods(content: string, lines: string[]): ParsedMethod[] {
  const methods: ParsedMethod[] = [];

  // Refined regex to match actual method declarations
  // This avoids matching return statements and ensures we get real methods
  const methodRegex = /^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?(?:virtual\s+)?(?:override\s+)?([\w<>[\]?]+)\s+([A-Z]\w+)\s*\(([\s\S]*?)\)\s*{/gm;

  let match;
  while ((match = methodRegex.exec(content)) !== null) {
    const startPos = match.index;
    const startLine = content.substring(0, startPos).split('\n').length;
    const methodName = match[2];
    const returnType = match[1];
    const parameters = match[3];

    // Skip if this looks like a return statement (common false positives)
    const lineBeforeMatch = content.substring(0, startPos).split('\n').slice(-1)[0] || '';
    if (lineBeforeMatch.trim().startsWith('return ')) {
      continue;
    }

    // Skip common controller return methods that aren't real methods
    const returnMethodNames = ['Ok', 'NotFound', 'BadRequest', 'StatusCode', 'CreatedAtAction',
                               'Redirect', 'RedirectToAction', 'View', 'PartialView', 'Json',
                               'Unauthorized', 'Forbid', 'NoContent', 'File', 'Content'];
    if (returnMethodNames.includes(methodName)) {
      continue;
    }

    // Extract attributes (lines above method that start with [)
    const attributes: string[] = [];
    for (let i = startLine - 2; i >= 0 && i < lines.length; i--) {
      const line = lines[i]?.trim() || '';
      if (line.startsWith('[') && line.includes(']')) {
        attributes.unshift(line);
      } else if (line && !line.startsWith('//') && !line.startsWith('///')) {
        break;
      }
    }

    // Find method end (matching closing brace)
    const methodStart = startPos + match[0].length;
    const endBrace = findMatchingBrace(content, methodStart - 1);
    const endLine = endBrace ? content.substring(0, endBrace).split('\n').length : startLine;

    const fullText = content.substring(startPos, endBrace ? endBrace + 1 : content.length);

    // Check for async keyword
    const isAsync = /\basync\s+/.test(match[0]);

    // Check for public keyword
    const isPublic = /\bpublic\s+/.test(match[0]);

    methods.push({
      name: methodName,
      signature: `${returnType} ${methodName}(${parameters})`,
      startLine,
      endLine,
      fullText,
      attributes,
      isPublic,
      isAsync,
      returnType,
      parameters,
    });
  }

  return methods;
}

/**
 * Extract properties from C# content
 */
function extractProperties(content: string, lines: string[]): ParsedProperty[] {
  const properties: ParsedProperty[] = [];

  // Regex to match property definitions
  const propertyRegex = /^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal)\s+(?:static\s+)?(?:readonly\s+)?([\w<>[\]?]+)\s+([A-Z]\w+)\s*{\s*get[^}]*?;(?:\s*set[^}]*?;)?\s*}/gm;

  let match;
  while ((match = propertyRegex.exec(content)) !== null) {
    const startPos = match.index;
    const startLine = content.substring(0, startPos).split('\n').length;
    const propName = match[2];
    const propType = match[1];

    // Extract attributes
    const attributes: string[] = [];
    for (let i = startLine - 2; i >= 0 && i < lines.length; i--) {
      const line = lines[i]?.trim() || '';
      if (line.startsWith('[') && line.includes(']')) {
        attributes.unshift(line);
      } else if (line && !line.startsWith('//') && !line.startsWith('///')) {
        break;
      }
    }

    // Parse accessibility
    const accessMatch = match[0].match(/\b(public|private|protected|internal)\b/);
    const accessibility = accessMatch ? accessMatch[1] : 'private';

    // Check for readonly
    const isReadOnly = /\breadonly\s+/.test(match[0]);

    // Check for getter/setter
    const hasGetter = /\bget\b/.test(match[0]);
    const hasSetter = /\bset\b/.test(match[0]);

    properties.push({
      name: propName,
      type: propType,
      attributes,
      accessibility,
      isReadOnly,
      hasGetter,
      hasSetter,
      fullText: match[0],
      startLine,
    });
  }

  return properties;
}

/**
 * Find matching closing brace for opening brace at given position
 * Handles strings, verbatim strings (@"..."), interpolated strings ($"..."), chars, and comments
 */
function findMatchingBrace(content: string, openPos: number): number | null {
  let depth = 1;
  let inString = false;
  let inVerbatimString = false;
  let inInterpolatedString = false;
  let inChar = false;
  let inComment = false;
  let inLineComment = false;
  let interpolatedStringBraceDepth = 0;

  for (let i = openPos + 1; i < content.length; i++) {
    const char = content[i];
    const prev = content[i - 1];
    const prev2 = i >= 2 ? content[i - 2] : '';
    const next = content[i + 1];

    // Handle line comments
    if (char === '/' && next === '/' && !inString && !inVerbatimString && !inInterpolatedString && !inChar && !inComment) {
      inLineComment = true;
      continue;
    }
    if (inLineComment && char === '\n') {
      inLineComment = false;
      continue;
    }
    if (inLineComment) continue;

    // Handle block comments
    if (char === '/' && next === '*' && !inString && !inVerbatimString && !inInterpolatedString && !inChar) {
      inComment = true;
      continue;
    }
    if (inComment && char === '*' && next === '/') {
      inComment = false;
      i++; // skip the /
      continue;
    }
    if (inComment) continue;

    // Handle verbatim strings (@"...")
    if (char === '"' && prev === '@' && !inString && !inInterpolatedString && !inChar) {
      inVerbatimString = true;
      continue;
    }
    if (inVerbatimString) {
      // In verbatim strings, "" is an escaped quote
      if (char === '"') {
        if (next === '"') {
          i++; // skip the second quote
          continue;
        } else {
          inVerbatimString = false;
        }
      }
      continue;
    }

    // Handle interpolated strings ($"...")
    if (char === '"' && prev === '$' && !inString && !inVerbatimString && !inChar) {
      inInterpolatedString = true;
      interpolatedStringBraceDepth = 0;
      continue;
    }
    if (inInterpolatedString) {
      if (char === '{') {
        interpolatedStringBraceDepth++;
      } else if (char === '}') {
        interpolatedStringBraceDepth--;
      } else if (char === '"' && prev !== '\\' && interpolatedStringBraceDepth === 0) {
        inInterpolatedString = false;
      }
      continue;
    }

    // Handle regular strings
    if (char === '"' && prev !== '\\' && prev !== '@' && prev !== '$' && !inChar) {
      inString = !inString;
      continue;
    }

    // Handle chars
    if (char === "'" && prev !== '\\' && !inString && !inVerbatimString && !inInterpolatedString) {
      inChar = !inChar;
      continue;
    }

    if (inString || inChar) continue;

    // Count braces
    if (char === '{') depth++;
    if (char === '}') {
      depth--;
      if (depth === 0) return i;
    }
  }

  return null;
}

// ============================================================================
// Razor/View Parsing Functions
// ============================================================================

/**
 * Parse Razor/.cshtml file to extract sections
 */
function parseRazorFile(filePath: string): ParsedRazorView | null {
  if (!existsSync(filePath)) {
    return null;
  }

  const content = readFileSync(filePath, 'utf-8');
  const lines = content.split('\n');

  // Extract @model declaration
  const modelMatch = content.match(/@model\s+([\w.]+)/);
  const modelDeclaration = modelMatch ? modelMatch[1] : '';

  // Extract @section blocks
  const sections = extractRazorSections(content, lines);

  return {
    modelDeclaration,
    sections,
    rawContent: content,
  };
}

/**
 * Extract @section blocks from Razor content
 */
function extractRazorSections(content: string, lines: string[]): ParsedRazorSection[] {
  const sections: ParsedRazorSection[] = [];

  // Regex to match @section Name {
  const sectionRegex = /@section\s+(\w+)\s*{/g;

  let match;
  while ((match = sectionRegex.exec(content)) !== null) {
    const startPos = match.index;
    const startLine = content.substring(0, startPos).split('\n').length;
    const sectionName = match[1];

    // Find the closing brace for this section
    const openBracePos = startPos + match[0].length - 1; // Position of the opening {
    const closeBracePos = findMatchingBraceRazor(content, openBracePos);

    if (closeBracePos !== null) {
      const endLine = content.substring(0, closeBracePos).split('\n').length;
      const fullText = content.substring(startPos, closeBracePos + 1);

      sections.push({
        name: sectionName,
        startLine,
        endLine,
        fullText,
      });
    }
  }

  return sections;
}

/**
 * Find matching closing brace for Razor sections
 * Similar to findMatchingBrace but simplified for Razor
 */
function findMatchingBraceRazor(content: string, openPos: number): number | null {
  let depth = 1;
  let inString = false;
  let inSingleQuote = false;

  for (let i = openPos + 1; i < content.length; i++) {
    const char = content[i];
    const prev = content[i - 1];

    // Handle strings (simplified for Razor)
    if (char === '"' && prev !== '\\') {
      inString = !inString;
      continue;
    }
    if (char === "'" && prev !== '\\') {
      inSingleQuote = !inSingleQuote;
      continue;
    }

    if (inString || inSingleQuote) continue;

    // Count braces
    if (char === '{') depth++;
    if (char === '}') {
      depth--;
      if (depth === 0) return i;
    }
  }

  return null;
}

/**
 * Analyze differences between generated and existing Razor views
 */
function analyzeRazorMerge(generated: ParsedRazorView, existing: ParsedRazorView) {
  const newSections: ParsedRazorSection[] = [];
  const changedSections: { generated: ParsedRazorSection; existing: ParsedRazorSection }[] = [];
  const unchangedSections: ParsedRazorSection[] = [];
  const conflicts: string[] = [];

  // Find new and changed sections
  for (const genSection of generated.sections) {
    const existingSection = existing.sections.find(s => s.name === genSection.name);

    if (!existingSection) {
      newSections.push(genSection);
    } else {
      // Compare content (normalize whitespace)
      const genContent = genSection.fullText.replace(/\s+/g, ' ').trim();
      const exContent = existingSection.fullText.replace(/\s+/g, ' ').trim();

      if (genContent !== exContent) {
        changedSections.push({ generated: genSection, existing: existingSection });
        conflicts.push(`Section content changed: ${genSection.name}`);
      } else {
        unchangedSections.push(genSection);
      }
    }
  }

  return {
    newSections,
    changedSections,
    unchangedSections,
    conflicts,
  };
}

/**
 * Prompt user for Razor section merge decision
 */
async function promptRazorSectionMerge(section: ParsedRazorSection, context: string): Promise<'add' | 'skip' | 'view' | 'quit'> {
  console.log('\n' + '─'.repeat(80));
  console.log(`✨ NEW SECTION FOUND: @section ${section.name}`);
  console.log('─'.repeat(80));
  console.log(`Context: ${context}`);
  console.log(`Lines: ${section.startLine} - ${section.endLine}`);

  const response = await prompts({
    type: 'select',
    name: 'action',
    message: 'What would you like to do?',
    choices: [
      { title: 'Add this section', value: 'add' },
      { title: 'Skip (don\'t add)', value: 'skip' },
      { title: 'View section code', value: 'view' },
      { title: 'Quit merge process', value: 'quit' },
    ],
  });

  if (response.action === 'view') {
    console.log('\n' + '─'.repeat(80));
    console.log('SECTION CODE:');
    console.log('─'.repeat(80));
    console.log(section.fullText);
    console.log('─'.repeat(80));

    // Ask again after showing code
    return promptRazorSectionMerge(section, context);
  }

  return response.action || 'skip';
}

/**
 * Insert new Razor section into existing content
 * Sections are added at the end of the file (before closing tags)
 */
function insertRazorSection(existingContent: string, newSection: ParsedRazorSection): string {
  // Insert section at the end of the file (simple strategy)
  return existingContent + '\n\n' + newSection.fullText;
}

// ============================================================================
// Merge Analysis
// ============================================================================

/**
 * Analyze differences between generated and existing code
 */
function analyzeMerge(generated: ParsedClass, existing: ParsedClass): MergeAnalysis {
  const analysis: MergeAnalysis = {
    newMethods: [],
    changedMethods: [],
    removedMethods: [],
    unchangedMethods: [],
    newProperties: [],
    changedProperties: [],
    conflicts: [],
  };

  // Find new and changed methods
  for (const genMethod of generated.methods) {
    const existingMethod = existing.methods.find(m => m.name === genMethod.name);

    if (!existingMethod) {
      analysis.newMethods.push(genMethod);
    } else {
      // Compare signatures (normalize whitespace)
      const genSig = genMethod.signature.replace(/\s+/g, ' ').trim();
      const exSig = existingMethod.signature.replace(/\s+/g, ' ').trim();

      if (genSig !== exSig) {
        analysis.changedMethods.push({ generated: genMethod, existing: existingMethod });
        analysis.conflicts.push(`Method signature changed: ${genMethod.name}`);
      } else {
        analysis.unchangedMethods.push(genMethod);
      }
    }
  }

  // Find removed methods (in existing but not in generated) - these are custom methods to preserve
  for (const existingMethod of existing.methods) {
    const generatedMethod = generated.methods.find(m => m.name === existingMethod.name);
    if (!generatedMethod) {
      analysis.removedMethods.push(existingMethod);
    }
  }

  // Find new and changed properties
  for (const genProp of generated.properties) {
    const existingProp = existing.properties.find(p => p.name === genProp.name);

    if (!existingProp) {
      analysis.newProperties.push(genProp);
    } else {
      // Compare types
      if (genProp.type !== existingProp.type) {
        analysis.changedProperties.push({ generated: genProp, existing: existingProp });
        analysis.conflicts.push(`Property type changed: ${genProp.name}`);
      }
    }
  }

  return analysis;
}

// ============================================================================
// Interactive Prompts
// ============================================================================

/**
 * Prompt user for merge decision on a method
 */
async function promptMethodMerge(method: ParsedMethod, context: string): Promise<'add' | 'skip' | 'view' | 'edit' | 'quit'> {
  console.log('\n' + '─'.repeat(80));
  console.log(`✨ NEW METHOD FOUND: ${method.name}`);
  console.log('─'.repeat(80));
  console.log(`Context: ${context}`);
  console.log(`Signature: ${method.signature}`);
  console.log(`Attributes: ${method.attributes.join(', ') || 'none'}`);
  console.log(`Public: ${method.isPublic}, Async: ${method.isAsync}`);

  const response = await prompts({
    type: 'select',
    name: 'action',
    message: 'What would you like to do?',
    choices: [
      { title: 'Add this method', value: 'add' },
      { title: 'Skip (don\'t add)', value: 'skip' },
      { title: 'View full method code', value: 'view' },
      { title: 'View diff', value: 'diff' },
      { title: 'Quit merge process', value: 'quit' },
    ],
  });

  if (response.action === 'view') {
    console.log('\n' + '─'.repeat(80));
    console.log('METHOD CODE:');
    console.log('─'.repeat(80));
    console.log(method.fullText);
    console.log('─'.repeat(80));

    // Ask again after showing code
    return promptMethodMerge(method, context);
  }

  return response.action || 'skip';
}

/**
 * Prompt user for merge decision on a property
 */
async function promptPropertyMerge(property: ParsedProperty, context: string): Promise<'add' | 'skip' | 'view' | 'quit'> {
  console.log('\n' + '─'.repeat(80));
  console.log(`✨ NEW PROPERTY FOUND: ${property.name}`);
  console.log('─'.repeat(80));
  console.log(`Context: ${context}`);
  console.log(`Type: ${property.type}`);
  console.log(`Attributes: ${property.attributes.join(', ') || 'none'}`);
  console.log(`Accessibility: ${property.accessibility}`);

  const response = await prompts({
    type: 'select',
    name: 'action',
    message: 'What would you like to do?',
    choices: [
      { title: 'Add this property', value: 'add' },
      { title: 'Skip (don\'t add)', value: 'skip' },
      { title: 'View full property code', value: 'view' },
      { title: 'Quit merge process', value: 'quit' },
    ],
  });

  if (response.action === 'view') {
    console.log('\n' + '─'.repeat(80));
    console.log('PROPERTY CODE:');
    console.log('─'.repeat(80));
    console.log(property.fullText);
    console.log('─'.repeat(80));

    // Ask again after showing code
    return promptPropertyMerge(property, context);
  }

  return response.action || 'skip';
}

/**
 * Prompt for conflict resolution
 */
async function promptConflictResolution(
  methodName: string,
  generatedSig: string,
  existingSig: string
): Promise<'keep' | 'replace' | 'skip'> {
  console.log('\n' + '⚠'.repeat(40));
  console.log(`⚠️  CONFLICT: Method signature mismatch for "${methodName}"`);
  console.log('⚠'.repeat(40));
  console.log(`Generated: ${generatedSig}`);
  console.log(`Existing:  ${existingSig}`);

  const response = await prompts({
    type: 'select',
    name: 'action',
    message: 'How should this conflict be resolved?',
    choices: [
      { title: 'Keep existing (recommended)', value: 'keep' },
      { title: 'Replace with generated', value: 'replace' },
      { title: 'Skip (leave unchanged)', value: 'skip' },
    ],
  });

  return response.action || 'keep';
}

/**
 * Show side-by-side diff
 */
function showDiff(generated: string, existing: string, title: string) {
  console.log('\n' + '═'.repeat(80));
  console.log(`📊 DIFF: ${title}`);
  console.log('═'.repeat(80));

  const diff = diffLines(existing, generated);

  diff.forEach((part) => {
    const prefix = part.added ? '+ ' : part.removed ? '- ' : '  ';
    const color = part.added ? '\x1b[32m' : part.removed ? '\x1b[31m' : '\x1b[0m';
    const lines = part.value.split('\n');

    lines.forEach((line) => {
      if (line) {
        console.log(color + prefix + line + '\x1b[0m');
      }
    });
  });

  console.log('═'.repeat(80) + '\n');
}

// ============================================================================
// Backup and Rollback
// ============================================================================

/**
 * Create backup of file before modification
 */
function createBackup(filePath: string): boolean {
  try {
    const backupPath = filePath + '.backup';
    copyFileSync(filePath, backupPath);
    console.log(`  💾 Backup created: ${backupPath}`);
    return true;
  } catch (error) {
    console.error(`  ❌ Failed to create backup: ${error}`);
    return false;
  }
}

/**
 * Restore file from backup
 */
function restoreFromBackup(filePath: string): boolean {
  try {
    const backupPath = filePath + '.backup';
    if (!existsSync(backupPath)) {
      console.error(`  ❌ Backup not found: ${backupPath}`);
      return false;
    }
    copyFileSync(backupPath, filePath);
    console.log(`  ✅ Restored from backup: ${filePath}`);
    return true;
  } catch (error) {
    console.error(`  ❌ Failed to restore backup: ${error}`);
    return false;
  }
}

/**
 * Perform rollback for all merged files
 */
async function performRollback(entity: string, uiPath: string, apiPath: string): Promise<void> {
  console.log(`\n📦 Searching for backup files for entity: ${entity}\n`);

  const possibleFiles = [
    join(uiPath, 'Controllers', `${entity}Controller.cs`),
    join(uiPath, 'Services', `${entity}Service.cs`),
    join(uiPath, 'Services', `I${entity}Service.cs`),
    join(uiPath, 'ViewModels', `${entity}EditViewModel.cs`),
    join(uiPath, 'ViewModels', `${entity}SearchViewModel.cs`),
    join(apiPath, 'src', 'Admin.Api', 'Controllers', `${entity}Controller.cs`),
    join(apiPath, 'src', 'Admin.Api', 'Services', `${entity}Service.cs`),
    join(apiPath, 'src', 'Admin.Api', 'Services', `I${entity}Service.cs`),
    join(apiPath, 'src', 'Admin.Api', 'Repositories', `${entity}Repository.cs`),
    join(apiPath, 'src', 'Admin.Api', 'Repositories', `I${entity}Repository.cs`),
  ];

  let restoredCount = 0;
  let notFoundCount = 0;

  for (const filePath of possibleFiles) {
    const backupPath = filePath + '.backup';
    if (existsSync(backupPath)) {
      const success = restoreFromBackup(filePath);
      if (success) {
        restoredCount++;
      }
    } else {
      notFoundCount++;
    }
  }

  console.log(`\n${'═'.repeat(80)}`);
  console.log('🔄 ROLLBACK SUMMARY');
  console.log(`${'═'.repeat(80)}`);
  console.log(`   ✅ Restored: ${restoredCount} file(s)`);
  console.log(`   ⏭️  No backup found: ${notFoundCount} file(s)`);
  console.log(`${'═'.repeat(80)}\n`);

  if (restoredCount > 0) {
    console.log('✅ Rollback complete!');
    console.log('\n📝 Files have been restored to their pre-merge state.\n');
  } else {
    console.log('⚠️  No backup files found to restore.');
    console.log('   Backups are created during merge operations.\n');
  }
}

// ============================================================================
// Smart Insertion Logic
// ============================================================================

/**
 * Find best insertion point for new method
 * Strategy: Always insert at the bottom (before class closing brace) to minimize merge conflicts
 */
function findInsertionPoint(
  existingContent: string,
  newMethod: ParsedMethod,
  existingMethods: ParsedMethod[]
): number {
  const lines = existingContent.split('\n');

  // Find the last method in the existing file
  if (existingMethods.length === 0) {
    // No existing methods, insert before class closing brace
    const lastBraceIndex = existingContent.lastIndexOf('}');
    return lastBraceIndex > 0 ? lastBraceIndex : existingContent.length;
  }

  // Always insert after the last method (at the bottom)
  const lastMethod = existingMethods[existingMethods.length - 1];

  // Find the line after the last method's closing brace
  let insertLine = lastMethod.endLine;

  // Skip any blank lines after the last method
  while (insertLine < lines.length && lines[insertLine]?.trim() === '') {
    insertLine++;
  }

  // Convert line number to character position
  // Insert before the class closing brace
  const contentUpToInsertPoint = lines.slice(0, insertLine).join('\n');
  return contentUpToInsertPoint.length;
}

/**
 * Insert new method into existing content
 */
function insertMethod(
  existingContent: string,
  newMethod: ParsedMethod,
  existingMethods: ParsedMethod[]
): string {
  const insertPos = findInsertionPoint(existingContent, newMethod, existingMethods);

  // Format the method with proper indentation
  const methodText = '\n\n' + newMethod.fullText + '\n';

  // Insert at position
  const before = existingContent.substring(0, insertPos);
  const after = existingContent.substring(insertPos);

  return before + methodText + after;
}

/**
 * Find best insertion point for new property
 * Strategy: Always insert at the bottom of property block (after last property, before methods)
 */
function findPropertyInsertionPoint(
  existingContent: string,
  existingProperties: ParsedProperty[],
  existingMethods: ParsedMethod[]
): number {
  const lines = existingContent.split('\n');

  // If there are existing properties, insert after the last one
  if (existingProperties.length > 0) {
    const lastProp = existingProperties[existingProperties.length - 1];
    // Find the end line of the last property
    let propEndLine = lastProp.startLine;

    // Look for the closing } of the property
    for (let i = propEndLine; i < lines.length; i++) {
      const line = lines[i] || '';
      if (line.includes('}') && !line.includes('{')) {
        // Skip blank lines after the property
        let insertLine = i + 1;
        while (insertLine < lines.length && lines[insertLine]?.trim() === '') {
          insertLine++;
        }
        return lines.slice(0, insertLine).join('\n').length;
      }
    }
  }

  // If no properties but there are methods, insert before first method
  if (existingMethods.length > 0) {
    const firstMethod = existingMethods[0];
    let lineIndex = firstMethod.startLine - 1;

    // Move up to find the start of attributes/comments
    while (lineIndex > 0) {
      const line = lines[lineIndex - 1]?.trim() || '';
      if (line.startsWith('[') || line.startsWith('//') || line.startsWith('///') || line === '') {
        lineIndex--;
      } else {
        break;
      }
    }

    return lines.slice(0, lineIndex).join('\n').length;
  }

  // No properties or methods, insert before class closing brace
  const lastBraceIndex = existingContent.lastIndexOf('}');
  return lastBraceIndex > 0 ? lastBraceIndex - 1 : existingContent.length;
}

/**
 * Insert new property into existing content
 */
function insertProperty(
  existingContent: string,
  newProperty: ParsedProperty,
  existingProperties: ParsedProperty[],
  existingMethods: ParsedMethod[]
): string {
  const insertPos = findPropertyInsertionPoint(existingContent, existingProperties, existingMethods);

  // Format the property with proper indentation (assuming 4 spaces or 1 tab)
  const indent = '    ';
  const propertyLines = newProperty.fullText.split('\n');
  const formattedProperty = '\n' + propertyLines.map(line => indent + line).join('\n') + '\n';

  // Insert at position
  const before = existingContent.substring(0, insertPos);
  const after = existingContent.substring(insertPos);

  return before + formattedProperty + after;
}

/**
 * Replace existing method with new implementation
 */
function replaceMethod(
  existingContent: string,
  oldMethod: ParsedMethod,
  newMethod: ParsedMethod
): string {
  const lines = existingContent.split('\n');

  // Find start line (including attributes)
  let startLine = oldMethod.startLine - 1;
  while (startLine > 0) {
    const line = lines[startLine - 1]?.trim() || '';
    if (line.startsWith('[') || line.startsWith('//') || line.startsWith('///') || line === '') {
      startLine--;
    } else {
      break;
    }
  }

  // Calculate character positions
  const startPos = lines.slice(0, startLine).join('\n').length;
  const endPos = lines.slice(0, oldMethod.endLine).join('\n').length;

  // Replace
  const before = existingContent.substring(0, startPos);
  const after = existingContent.substring(endPos);

  return before + '\n' + newMethod.fullText + after;
}

// ============================================================================
// Using Statement Management
// ============================================================================

/**
 * Merge and deduplicate using statements
 */
function mergeUsingStatements(generatedUsings: string[], existingUsings: string[]): string[] {
  const allUsings = new Set([...existingUsings, ...generatedUsings]);
  return Array.from(allUsings).sort();
}

/**
 * Update using statements in file content
 */
function updateUsingStatements(content: string, newUsings: string[]): string {
  // Remove existing using statements
  const lines = content.split('\n');
  const nonUsingLines: string[] = [];
  let inUsings = false;
  let firstNonUsingIndex = 0;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i] || '';
    const trimmed = line.trim();

    if (trimmed.startsWith('using ') && trimmed.endsWith(';')) {
      inUsings = true;
      continue; // Skip existing using statements
    } else if (inUsings && trimmed === '') {
      continue; // Skip blank lines in using block
    } else {
      if (inUsings) {
        firstNonUsingIndex = nonUsingLines.length;
        inUsings = false;
      }
      nonUsingLines.push(line);
    }
  }

  // Insert new using statements at the beginning
  const usingBlock = newUsings.map(u => `using ${u};`).join('\n') + '\n';

  // Find first non-comment line to insert after
  let insertIndex = 0;
  for (let i = 0; i < nonUsingLines.length; i++) {
    const line = nonUsingLines[i]?.trim() || '';
    if (!line.startsWith('//') && line !== '') {
      insertIndex = i;
      break;
    }
  }

  const result = [
    ...nonUsingLines.slice(0, insertIndex),
    usingBlock,
    ...nonUsingLines.slice(insertIndex),
  ].join('\n');

  return result;
}

// ============================================================================
// File Copy Operations
// ============================================================================

/**
 * Prompt user to copy a new file
 */
async function promptFileCopy(filePath: string, fileType: string): Promise<'copy' | 'skip' | 'quit'> {
  console.log('\n' + '─'.repeat(80));
  console.log(`📄 NEW FILE FOUND: ${filePath}`);
  console.log('─'.repeat(80));
  console.log(`Type: ${fileType}`);
  console.log(`This file doesn't exist in the target project yet.`);

  const response = await prompts({
    type: 'select',
    name: 'action',
    message: 'What would you like to do?',
    choices: [
      { title: 'Copy this file to target', value: 'copy' },
      { title: 'Skip (don\'t copy)', value: 'skip' },
      { title: 'Quit process', value: 'quit' },
    ],
  });

  return response.action || 'skip';
}

/**
 * Copy a new file to target location
 */
async function copyNewFile(
  generatedPath: string,
  targetPath: string,
  options: MergeOptions
): Promise<FileMergeResult> {
  const result: FileMergeResult = {
    filePath: targetPath,
    status: 'skipped',
    methodsAdded: 0,
    methodsChanged: 0,
    methodsPreserved: 0,
    propertiesAdded: 0,
    conflicts: [],
  };

  if (!existsSync(generatedPath)) {
    result.status = 'error';
    result.error = 'Generated file not found';
    return result;
  }

  console.log(`\n${'═'.repeat(80)}`);
  console.log(`📄 New File: ${targetPath}`);
  console.log(`${'═'.repeat(80)}`);

  // Handle dry-run mode
  if (options.mode === 'dry-run') {
    result.status = 'skipped';
    console.log(`\n  [DRY RUN] Would copy new file\n`);
    return result;
  }

  let action: 'copy' | 'skip' | 'quit' = 'copy';

  if (options.mode === 'interactive') {
    action = await promptFileCopy(targetPath, targetPath.includes('Controllers') ? 'Controller' : 'Other');
  }

  if (action === 'quit') {
    console.log('\n⏸️  Copy process cancelled by user');
    result.status = 'skipped';
    return result;
  }

  if (action === 'copy') {
    try {
      // Create target directory if it doesn't exist
      const targetDir = dirname(targetPath);
      if (!existsSync(targetDir)) {
        mkdirSync(targetDir, { recursive: true });
        console.log(`  📁 Created directory: ${targetDir}`);
      }

      // Copy the file
      copyFileSync(generatedPath, targetPath);
      result.status = 'copied';
      console.log(`  ✅ Copied file: ${targetPath}`);
    } catch (error) {
      result.status = 'error';
      result.error = `Failed to copy file: ${error}`;
      console.error(`  ❌ ${result.error}`);
    }
  } else {
    console.log(`  ⏭️  Skipped file: ${targetPath}`);
  }

  return result;
}

// ============================================================================
// Merge Execution
// ============================================================================

/**
 * Execute merge for a single file
 */
async function mergeFile(
  generatedPath: string,
  existingPath: string,
  options: MergeOptions
): Promise<FileMergeResult> {
  const result: FileMergeResult = {
    filePath: existingPath,
    status: 'skipped',
    methodsAdded: 0,
    methodsChanged: 0,
    methodsPreserved: 0,
    propertiesAdded: 0,
    conflicts: [],
  };

  // Parse both files
  const generated = parseCSharpFile(generatedPath);
  const existing = parseCSharpFile(existingPath);

  if (!generated || !existing) {
    result.status = 'error';
    result.error = 'Failed to parse files';
    return result;
  }

  // Analyze merge
  const analysis = analyzeMerge(generated, existing);

  console.log(`\n${'═'.repeat(80)}`);
  console.log(`📄 Merging: ${existingPath}`);
  console.log(`${'═'.repeat(80)}`);
  console.log(`   ✨ ${analysis.newMethods.length} new method(s)`);
  console.log(`   🔷 ${analysis.newProperties.length} new property(ies)`);
  console.log(`   📝 ${analysis.changedMethods.length} changed method(s)`);
  console.log(`   💾 ${analysis.removedMethods.length} custom method(s) to preserve`);
  console.log(`   ⚠️  ${analysis.conflicts.length} conflict(s)`);

  // Handle dry-run mode
  if (options.mode === 'dry-run') {
    result.status = 'skipped';
    result.methodsAdded = analysis.newMethods.length;
    result.propertiesAdded = analysis.newProperties.length;
    result.methodsPreserved = analysis.removedMethods.length;
    result.conflicts = analysis.conflicts;
    console.log(`\n  [DRY RUN] Would merge ${analysis.newMethods.length} method(s) and ${analysis.newProperties.length} property(ies)\n`);
    return result;
  }

  // Create backup
  if (!createBackup(existingPath)) {
    result.status = 'error';
    result.error = 'Failed to create backup';
    return result;
  }

  let modifiedContent = existing.rawContent;
  let methodsAdded = 0;
  let propertiesAdded = 0;

  // Re-parse after each change to get accurate positions
  let currentParsed = existing;

  // Process new methods
  for (const method of analysis.newMethods) {
    let action: 'add' | 'skip' | 'view' | 'edit' | 'quit' = 'add';

    if (options.mode === 'interactive') {
      action = await promptMethodMerge(method, existingPath);
    }

    if (action === 'quit') {
      console.log('\n⏸️  Merge process cancelled by user');
      result.status = 'skipped';
      return result;
    }

    if (action === 'add') {
      modifiedContent = insertMethod(modifiedContent, method, currentParsed.methods);
      methodsAdded++;
      console.log(`  ✅ Added method: ${method.name}`);

      // Re-parse to update positions
      const tempFile = existingPath + '.temp';
      writeFileSync(tempFile, modifiedContent, 'utf-8');
      const reparsed = parseCSharpFile(tempFile);
      if (reparsed) {
        currentParsed = { ...reparsed, rawContent: modifiedContent };
      }
    } else {
      console.log(`  ⏭️  Skipped method: ${method.name}`);
    }
  }

  // Process new properties
  for (const property of analysis.newProperties) {
    let action: 'add' | 'skip' | 'view' | 'quit' = 'add';

    if (options.mode === 'interactive') {
      action = await promptPropertyMerge(property, existingPath);
    }

    if (action === 'quit') {
      console.log('\n⏸️  Merge process cancelled by user');
      result.status = 'skipped';
      return result;
    }

    if (action === 'add') {
      modifiedContent = insertProperty(modifiedContent, property, currentParsed.properties, currentParsed.methods);
      propertiesAdded++;
      console.log(`  ✅ Added property: ${property.name}`);

      // Re-parse to update positions
      const tempFile = existingPath + '.temp';
      writeFileSync(tempFile, modifiedContent, 'utf-8');
      const reparsed = parseCSharpFile(tempFile);
      if (reparsed) {
        currentParsed = { ...reparsed, rawContent: modifiedContent };
      }
    } else {
      console.log(`  ⏭️  Skipped property: ${property.name}`);
    }
  }

  // Handle conflicts
  for (const { generated, existing: existingMethod } of analysis.changedMethods) {
    let action: 'keep' | 'replace' | 'skip' = 'keep';

    if (options.mode === 'interactive') {
      action = await promptConflictResolution(
        generated.name,
        generated.signature,
        existingMethod.signature
      );
    } else {
      action = options.conflictStrategy === 'keep-existing' ? 'keep' : 'replace';
    }

    if (action === 'replace') {
      modifiedContent = replaceMethod(modifiedContent, existingMethod, generated);
      result.methodsChanged++;
      console.log(`  ✅ Replaced method: ${generated.name}`);

      // Re-parse to update positions
      const tempFile = existingPath + '.temp';
      writeFileSync(tempFile, modifiedContent, 'utf-8');
      const reparsed = parseCSharpFile(tempFile);
      if (reparsed) {
        currentParsed = { ...reparsed, rawContent: modifiedContent };
      }
    } else {
      console.log(`  💾 Keeping existing: ${existingMethod.name}`);
    }
  }

  // Merge using statements
  const mergedUsings = mergeUsingStatements(generated.usings, existing.usings);
  if (mergedUsings.length > existing.usings.length) {
    modifiedContent = updateUsingStatements(modifiedContent, mergedUsings);
    console.log(`  ✅ Added ${mergedUsings.length - existing.usings.length} using statement(s)`);
  }

  // Clean up temp file if it exists
  const tempFile = existingPath + '.temp';
  if (existsSync(tempFile)) {
    try {
      unlinkSync(tempFile);
    } catch (error) {
      // Ignore cleanup errors
    }
  }

  // Write modified content
  if (methodsAdded > 0 || propertiesAdded > 0) {
    writeFileSync(existingPath, modifiedContent, 'utf-8');
    result.status = 'merged';
    result.methodsAdded = methodsAdded;
    result.propertiesAdded = propertiesAdded;
    result.methodsPreserved = analysis.removedMethods.length;
    console.log(`\n  ✅ Merge complete: ${methodsAdded} method(s) and ${propertiesAdded} property(ies) added`);
  } else {
    result.status = 'skipped';
    console.log(`\n  ℹ️  No changes made`);
  }

  return result;
}

/**
 * Execute merge for a Razor view file
 */
async function mergeRazorFile(
  generatedPath: string,
  existingPath: string,
  options: MergeOptions
): Promise<FileMergeResult> {
  const result: FileMergeResult = {
    filePath: existingPath,
    status: 'skipped',
    methodsAdded: 0,
    methodsChanged: 0,
    methodsPreserved: 0,
    propertiesAdded: 0,
    conflicts: [],
  };

  // Parse both files
  const generated = parseRazorFile(generatedPath);
  const existing = parseRazorFile(existingPath);

  if (!generated || !existing) {
    result.status = 'error';
    result.error = 'Failed to parse Razor files';
    return result;
  }

  // Analyze merge
  const analysis = analyzeRazorMerge(generated, existing);

  console.log(`\n${'═'.repeat(80)}`);
  console.log(`📄 Merging Razor View: ${existingPath}`);
  console.log(`${'═'.repeat(80)}`);
  console.log(`   ✨ ${analysis.newSections.length} new section(s)`);
  console.log(`   📝 ${analysis.changedSections.length} changed section(s)`);
  console.log(`   ⚠️  ${analysis.conflicts.length} conflict(s)`);

  // Handle dry-run mode
  if (options.mode === 'dry-run') {
    result.status = 'skipped';
    result.methodsAdded = analysis.newSections.length;  // Reusing methodsAdded for sections count
    result.conflicts = analysis.conflicts;
    console.log(`\n  [DRY RUN] Would merge ${analysis.newSections.length} section(s)\n`);
    return result;
  }

  // Create backup
  if (!createBackup(existingPath)) {
    result.status = 'error';
    result.error = 'Failed to create backup';
    return result;
  }

  let modifiedContent = existing.rawContent;
  let sectionsAdded = 0;

  // Process new sections
  for (const section of analysis.newSections) {
    let action: 'add' | 'skip' | 'view' | 'quit' = 'add';

    if (options.mode === 'interactive') {
      action = await promptRazorSectionMerge(section, existingPath);
    }

    if (action === 'quit') {
      console.log('\n⏸️  Merge process cancelled by user');
      result.status = 'skipped';
      return result;
    }

    if (action === 'add') {
      modifiedContent = insertRazorSection(modifiedContent, section);
      sectionsAdded++;
      console.log(`  ✅ Added section: @section ${section.name}`);
    } else {
      console.log(`  ⏭️  Skipped section: @section ${section.name}`);
    }
  }

  // Handle changed sections (conflicts)
  for (const { generated, existing: existingSection } of analysis.changedSections) {
    console.log(`  ⚠️  Section conflict: @section ${generated.name} (keeping existing)`);
    // For now, always keep existing sections (custom code preservation)
    // Future: Add prompt for section replacement
  }

  // Write modified content
  if (sectionsAdded > 0) {
    writeFileSync(existingPath, modifiedContent, 'utf-8');
    result.status = 'merged';
    result.methodsAdded = sectionsAdded;  // Reusing methodsAdded for sections count
    console.log(`\n  ✅ Merge complete: ${sectionsAdded} section(s) added`);
  } else {
    result.status = 'skipped';
    console.log(`\n  ℹ️  No changes made`);
  }

  return result;
}

// ============================================================================
// Main Entry Point
// ============================================================================

async function parseOptions(): Promise<MergeOptions> {
  const args = Bun.argv.slice(2);

  let entity = '';
  let mode: 'interactive' | 'auto' | 'dry-run' = 'interactive';
  let conflictStrategy: 'prompt' | 'keep-existing' | 'use-generated' = 'prompt';
  let rollback = false;

  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--entity' && args[i + 1]) {
      entity = args[i + 1];
      i++;
    } else if (args[i] === '--dry-run') {
      mode = 'dry-run';
    } else if (args[i] === '--auto') {
      mode = 'auto';
    } else if (args[i] === '--conflict-strategy' && args[i + 1]) {
      conflictStrategy = args[i + 1] as any;
      i++;
    } else if (args[i] === '--rollback') {
      rollback = true;
    }
  }

  if (!entity) {
    console.error('❌ Error: --entity parameter is required');
    console.error('\nUsage:');
    console.error('  bun run agents/interactive-template-merge.ts --entity "Customer"');
    console.error('  bun run agents/interactive-template-merge.ts --entity "Customer" --dry-run');
    console.error('  bun run agents/interactive-template-merge.ts --entity "Customer" --auto');
    console.error('  bun run agents/interactive-template-merge.ts --rollback --entity "Customer"');
    process.exit(1);
  }

  return {
    entity,
    mode,
    conflictStrategy,
    fileTypes: ['cs'],
    rollback,
  };
}

async function main() {
  const options = await parseOptions();

  console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║            INTERACTIVE TEMPLATE MERGE AGENT                                ║
║                                                                            ║
║  Entity: ${options.entity.padEnd(68, ' ')}║
║  Mode: ${options.mode.padEnd(70, ' ')}║
╚════════════════════════════════════════════════════════════════════════════╝
  `);

  // Define paths (needed for both merge and rollback)
  const templateRoot = join(projectRoot, 'output', options.entity, 'Templates');
  const uiPath = getAdminUiPath();
  const apiPath = getAdminApiPath();
  const sharedPath = getSharedProjectPath();

  // Handle rollback
  if (options.rollback) {
    console.log('🔄 Rolling back...');
    await performRollback(options.entity, uiPath, apiPath);
    return;
  }

  // Define all possible file operations (including shared project)
  const fileOperations: FileOperation[] = [
    // API Controllers
    {
      generated: join(templateRoot, 'api', 'Controllers', `${options.entity}Controller.cs`),
      existing: join(apiPath, 'src', 'Admin.Api', 'Controllers', `${options.entity}Controller.cs`),
      type: 'api',
      exists: false,
    },
    // API Services
    {
      generated: join(templateRoot, 'api', 'Services', `${options.entity}Service.cs`),
      existing: join(apiPath, 'src', 'Admin.Api', 'Services', `${options.entity}Service.cs`),
      type: 'api',
      exists: false,
    },
    {
      generated: join(templateRoot, 'api', 'Services', `I${options.entity}Service.cs`),
      existing: join(apiPath, 'src', 'Admin.Api', 'Services', `I${options.entity}Service.cs`),
      type: 'api',
      exists: false,
    },
    // API Repositories
    {
      generated: join(templateRoot, 'api', 'Repositories', `${options.entity}Repository.cs`),
      existing: join(apiPath, 'src', 'Admin.Api', 'Repositories', `${options.entity}Repository.cs`),
      type: 'api',
      exists: false,
    },
    {
      generated: join(templateRoot, 'api', 'Repositories', `I${options.entity}Repository.cs`),
      existing: join(apiPath, 'src', 'Admin.Api', 'Repositories', `I${options.entity}Repository.cs`),
      type: 'api',
      exists: false,
    },
    // Shared DTOs
    {
      generated: join(templateRoot, 'shared', 'Dto', `${options.entity}Dto.cs`),
      existing: join(sharedPath, 'Dto', `${options.entity}Dto.cs`),
      type: 'shared',
      exists: false,
    },
    {
      generated: join(templateRoot, 'shared', 'Dto', `${options.entity}SearchRequest.cs`),
      existing: join(sharedPath, 'Dto', `${options.entity}SearchRequest.cs`),
      type: 'shared',
      exists: false,
    },
    // UI Controllers
    {
      generated: join(templateRoot, 'ui', 'Controllers', `${options.entity}Controller.cs`),
      existing: join(uiPath, 'Controllers', `${options.entity}Controller.cs`),
      type: 'ui',
      exists: false,
    },
    // UI Services
    {
      generated: join(templateRoot, 'ui', 'Services', `${options.entity}Service.cs`),
      existing: join(uiPath, 'Services', `${options.entity}Service.cs`),
      type: 'ui',
      exists: false,
    },
    {
      generated: join(templateRoot, 'ui', 'Services', `I${options.entity}Service.cs`),
      existing: join(uiPath, 'Services', `I${options.entity}Service.cs`),
      type: 'ui',
      exists: false,
    },
    // UI ViewModels
    {
      generated: join(templateRoot, 'ui', 'ViewModels', `${options.entity}EditViewModel.cs`),
      existing: join(uiPath, 'ViewModels', `${options.entity}EditViewModel.cs`),
      type: 'ui',
      exists: false,
    },
    {
      generated: join(templateRoot, 'ui', 'ViewModels', `${options.entity}SearchViewModel.cs`),
      existing: join(uiPath, 'ViewModels', `${options.entity}SearchViewModel.cs`),
      type: 'ui',
      exists: false,
    },
    // UI Views (Razor)
    {
      generated: join(templateRoot, 'ui', 'Views', 'Index.cshtml'),
      existing: join(uiPath, 'Views', options.entity, 'Index.cshtml'),
      type: 'ui',
      exists: false,
    },
    {
      generated: join(templateRoot, 'ui', 'Views', 'Edit.cshtml'),
      existing: join(uiPath, 'Views', options.entity, 'Edit.cshtml'),
      type: 'ui',
      exists: false,
    },
  ];

  // Check which files exist and categorize them
  const filesToMerge: FileOperation[] = [];
  const filesToCopy: FileOperation[] = [];

  for (const op of fileOperations) {
    if (existsSync(op.generated)) {
      if (existsSync(op.existing)) {
        // File exists in both - merge it
        op.exists = true;
        filesToMerge.push(op);
      } else {
        // File only in generated - copy it
        op.exists = false;
        filesToCopy.push(op);
      }
    }
  }

  if (filesToMerge.length === 0 && filesToCopy.length === 0) {
    console.error('\n❌ No files found to merge or copy');
    console.error(`   Checked: ${templateRoot}`);
    console.error(`   Targets: ${apiPath}, ${uiPath}, ${sharedPath}`);
    return;
  }

  console.log(`\n📦 Found ${filesToMerge.length} file(s) to merge and ${filesToCopy.length} new file(s) to copy\n`);

  // Process all operations
  const results: FileMergeResult[] = [];

  // First, merge existing files
  for (const file of filesToMerge) {
    // Dispatch based on file extension
    const ext = file.generated.toLowerCase().endsWith('.cshtml') ? 'cshtml' : 'cs';

    let result: FileMergeResult;
    if (ext === 'cshtml') {
      result = await mergeRazorFile(file.generated, file.existing, options);
    } else {
      result = await mergeFile(file.generated, file.existing, options);
    }
    results.push(result);
  }

  // Then, copy new files
  for (const file of filesToCopy) {
    const result = await copyNewFile(file.generated, file.existing, options);
    results.push(result);
  }

  // Summary
  console.log(`\n${'═'.repeat(80)}`);
  console.log('📊 MERGE & COPY SUMMARY');
  console.log(`${'═'.repeat(80)}`);

  const merged = results.filter(r => r.status === 'merged').length;
  const copied = results.filter(r => r.status === 'copied').length;
  const skipped = results.filter(r => r.status === 'skipped').length;
  const errors = results.filter(r => r.status === 'error').length;
  const totalMethodsAdded = results.reduce((sum, r) => sum + r.methodsAdded, 0);
  const totalPropertiesAdded = results.reduce((sum, r) => sum + r.propertiesAdded, 0);
  const totalConflicts = results.reduce((sum, r) => sum + r.conflicts.length, 0);

  console.log(`   ✅ Merged: ${merged} file(s)`);
  console.log(`   📄 Copied: ${copied} new file(s)`);
  console.log(`   ⏭️  Skipped: ${skipped} file(s)`);
  console.log(`   ❌ Errors: ${errors} file(s)`);
  console.log(`   ✨ Methods added: ${totalMethodsAdded}`);
  console.log(`   🔷 Properties added: ${totalPropertiesAdded}`);
  console.log(`   ⚠️  Conflicts: ${totalConflicts}`);
  console.log(`${'═'.repeat(80)}\n`);

  if (merged > 0 || copied > 0) {
    console.log('✅ Operation complete!');
    console.log('\n📝 Next steps:');
    console.log('   1. Review merged/copied files');
    console.log('   2. Compile and test');
    console.log('   3. If issues with merged files, rollback with: --rollback');
    console.log('   4. Commit changes when satisfied\n');
  }
}

await main();
