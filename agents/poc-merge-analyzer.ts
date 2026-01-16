#!/usr/bin/env bun
/**
 * POC: Merge Analyzer - Proof of Concept
 *
 * Tests if we can reliably parse C# files and detect merge conflicts
 * before building the full interactive merge agent.
 *
 * Usage:
 *   bun run agents/poc-merge-analyzer.ts --generated "./output/Customer/Templates/api/Controllers/CustomerController.cs" --existing "C:/Dev/BargeOps.Admin.Mono/src/BargeOps.UI/Controllers/CustomerController.cs"
 */

import { readFileSync, existsSync } from 'fs';
import { parsedArgs } from '../lib/flags';

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
}

interface ParsedClass {
  name: string;
  methods: ParsedMethod[];
  properties: ParsedProperty[];
  usings: string[];
  namespace: string;
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

/**
 * Parse C# file to extract class structure
 */
function parseCSharpFile(filePath: string): ParsedClass | null {
  if (!existsSync(filePath)) {
    console.error(`❌ File not found: ${filePath}`);
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

  // Extract methods
  const methods = extractMethods(content, lines);

  // Extract properties
  const properties = extractProperties(content, lines);

  return {
    name: className,
    methods,
    properties,
    usings,
    namespace,
  };
}

/**
 * Extract all method definitions from C# content
 */
function extractMethods(content: string, lines: string[]): ParsedMethod[] {
  const methods: ParsedMethod[] = [];

  // Regex to match method signatures
  // Matches: [attributes] public/private async? ReturnType MethodName(params) { ... }
  const methodRegex = /(?:^\s*\[[\w\s(),="]+\]\s*)*^\s*(public|private|protected|internal)?\s*(static)?\s*(async)?\s*([\w<>[\]?]+)\s+(\w+)\s*\(([\s\S]*?)\)\s*{/gm;

  let match;
  while ((match = methodRegex.exec(content)) !== null) {
    const startPos = match.index;
    const startLine = content.substring(0, startPos).split('\n').length;

    // Extract attributes (lines above method that start with [)
    const attributes: string[] = [];
    for (let i = startLine - 2; i >= 0; i--) {
      const line = lines[i]?.trim() || '';
      if (line.startsWith('[') && line.endsWith(']')) {
        attributes.unshift(line);
      } else if (line && !line.startsWith('//')) {
        break;
      }
    }

    // Find method end (matching closing brace)
    const methodStart = startPos + match[0].length;
    const endBrace = findMatchingBrace(content, methodStart - 1);
    const endLine = endBrace ? content.substring(0, endBrace).split('\n').length : startLine;

    const fullText = content.substring(startPos, endBrace || content.length);

    methods.push({
      name: match[5],
      signature: `${match[4]} ${match[5]}(${match[6]})`,
      startLine,
      endLine,
      fullText,
      attributes,
      isPublic: match[1] === 'public',
      isAsync: match[3] === 'async',
      returnType: match[4],
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
  const propertyRegex = /(?:^\s*\[[\w\s(),="]+\]\s*)*^\s*(public|private|protected|internal)?\s*(static)?\s*(readonly)?\s*([\w<>[\]?]+)\s+(\w+)\s*{\s*(get[^}]*)?;\s*(set[^}]*)?;\s*}/gm;

  let match;
  while ((match = propertyRegex.exec(content)) !== null) {
    const startPos = match.index;
    const startLine = content.substring(0, startPos).split('\n').length;

    // Extract attributes
    const attributes: string[] = [];
    for (let i = startLine - 2; i >= 0; i--) {
      const line = lines[i]?.trim() || '';
      if (line.startsWith('[') && line.endsWith(']')) {
        attributes.unshift(line);
      } else if (line && !line.startsWith('//')) {
        break;
      }
    }

    properties.push({
      name: match[5],
      type: match[4],
      attributes,
      accessibility: match[1] || 'private',
      isReadOnly: match[3] === 'readonly',
      hasGetter: !!match[6],
      hasSetter: !!match[7],
      fullText: match[0],
    });
  }

  return properties;
}

/**
 * Find matching closing brace for opening brace at given position
 */
function findMatchingBrace(content: string, openPos: number): number | null {
  let depth = 1;
  let inString = false;
  let inChar = false;
  let inComment = false;
  let inLineComment = false;

  for (let i = openPos + 1; i < content.length; i++) {
    const char = content[i];
    const prev = content[i - 1];
    const next = content[i + 1];

    // Handle line comments
    if (char === '/' && next === '/' && !inString && !inChar && !inComment) {
      inLineComment = true;
      continue;
    }
    if (inLineComment && char === '\n') {
      inLineComment = false;
      continue;
    }
    if (inLineComment) continue;

    // Handle block comments
    if (char === '/' && next === '*' && !inString && !inChar) {
      inComment = true;
      continue;
    }
    if (inComment && char === '*' && next === '/') {
      inComment = false;
      i++; // skip the /
      continue;
    }
    if (inComment) continue;

    // Handle strings
    if (char === '"' && prev !== '\\' && !inChar) {
      inString = !inString;
      continue;
    }

    // Handle chars
    if (char === "'" && prev !== '\\' && !inString) {
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
      // Compare signatures
      if (genMethod.signature !== existingMethod.signature) {
        analysis.changedMethods.push({ generated: genMethod, existing: existingMethod });
        analysis.conflicts.push(`Method signature changed: ${genMethod.name}`);
      } else {
        analysis.unchangedMethods.push(genMethod);
      }
    }
  }

  // Find removed methods (in existing but not in generated)
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

/**
 * Print analysis results
 */
function printAnalysis(analysis: MergeAnalysis) {
  console.log('\n' + '═'.repeat(80));
  console.log('📊 MERGE ANALYSIS RESULTS');
  console.log('═'.repeat(80) + '\n');

  // Summary
  console.log('📋 Summary:');
  console.log(`   ✨ New methods: ${analysis.newMethods.length}`);
  console.log(`   📝 Changed methods: ${analysis.changedMethods.length}`);
  console.log(`   🗑️  Removed methods: ${analysis.removedMethods.length}`);
  console.log(`   ✅ Unchanged methods: ${analysis.unchangedMethods.length}`);
  console.log(`   ✨ New properties: ${analysis.newProperties.length}`);
  console.log(`   📝 Changed properties: ${analysis.changedProperties.length}`);
  console.log(`   ⚠️  Conflicts: ${analysis.conflicts.length}\n`);

  // New methods
  if (analysis.newMethods.length > 0) {
    console.log('─'.repeat(80));
    console.log('✨ NEW METHODS (would be added to existing file):');
    console.log('─'.repeat(80));
    for (const method of analysis.newMethods) {
      console.log(`\n📍 ${method.name}`);
      console.log(`   Signature: ${method.signature}`);
      console.log(`   Attributes: ${method.attributes.join(', ') || 'none'}`);
      console.log(`   Lines: ${method.startLine}-${method.endLine}`);
      console.log(`   Public: ${method.isPublic}, Async: ${method.isAsync}`);
    }
    console.log();
  }

  // Changed methods
  if (analysis.changedMethods.length > 0) {
    console.log('─'.repeat(80));
    console.log('📝 CHANGED METHODS (signature conflicts):');
    console.log('─'.repeat(80));
    for (const { generated, existing } of analysis.changedMethods) {
      console.log(`\n⚠️  ${generated.name}`);
      console.log(`   Generated: ${generated.signature}`);
      console.log(`   Existing:  ${existing.signature}`);
    }
    console.log();
  }

  // Removed methods
  if (analysis.removedMethods.length > 0) {
    console.log('─'.repeat(80));
    console.log('🗑️  REMOVED METHODS (in existing but not in generated):');
    console.log('─'.repeat(80));
    for (const method of analysis.removedMethods) {
      console.log(`\n💾 ${method.name} (will be PRESERVED)`);
      console.log(`   Signature: ${method.signature}`);
      console.log(`   Note: Custom method - not in generated template`);
    }
    console.log();
  }

  // New properties
  if (analysis.newProperties.length > 0) {
    console.log('─'.repeat(80));
    console.log('✨ NEW PROPERTIES (would be added):');
    console.log('─'.repeat(80));
    for (const prop of analysis.newProperties) {
      console.log(`\n📍 ${prop.name}: ${prop.type}`);
      console.log(`   Attributes: ${prop.attributes.join(', ') || 'none'}`);
      console.log(`   Access: ${prop.accessibility}, ReadOnly: ${prop.isReadOnly}`);
    }
    console.log();
  }

  // Conflicts
  if (analysis.conflicts.length > 0) {
    console.log('─'.repeat(80));
    console.log('⚠️  CONFLICTS (require manual resolution):');
    console.log('─'.repeat(80));
    for (const conflict of analysis.conflicts) {
      console.log(`   ❗ ${conflict}`);
    }
    console.log();
  }

  // Conclusion
  console.log('═'.repeat(80));
  if (analysis.conflicts.length === 0 && analysis.newMethods.length > 0) {
    console.log('✅ MERGE IS FEASIBLE');
    console.log(`   Can safely add ${analysis.newMethods.length} new method(s)`);
    console.log(`   ${analysis.removedMethods.length} custom method(s) will be preserved`);
  } else if (analysis.conflicts.length > 0) {
    console.log('⚠️  MERGE REQUIRES MANUAL REVIEW');
    console.log(`   ${analysis.conflicts.length} conflict(s) need resolution`);
  } else {
    console.log('ℹ️  NO CHANGES NEEDED');
    console.log('   Files are identical or existing has all features');
  }
  console.log('═'.repeat(80) + '\n');
}

/**
 * Main POC function
 */
async function main() {
  // Get args from command line (Bun.argv includes script name)
  const args = Bun.argv.slice(2);

  let generatedPath = '';
  let existingPath = '';

  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--generated' && args[i + 1]) {
      generatedPath = args[i + 1];
      i++;
    } else if (args[i] === '--existing' && args[i + 1]) {
      existingPath = args[i + 1];
      i++;
    }
  }

  if (!generatedPath || !existingPath) {
    console.error('❌ Usage: bun run agents/poc-merge-analyzer.ts --generated <path> --existing <path>');
    console.error('\nExample:');
    console.error('  bun run agents/poc-merge-analyzer.ts \\');
    console.error('    --generated "./output/Customer/Templates/api/Controllers/CustomerController.cs" \\');
    console.error('    --existing "C:/Dev/BargeOps.Admin.Mono/src/BargeOps.UI/Controllers/CustomerController.cs"');
    process.exit(1);
  }

  console.log('\n🔍 POC: Merge Analyzer');
  console.log('─'.repeat(80));
  console.log(`📄 Generated: ${generatedPath}`);
  console.log(`📄 Existing:  ${existingPath}`);

  // Parse both files
  console.log('\n⏳ Parsing files...');
  const generated = parseCSharpFile(generatedPath);
  const existing = parseCSharpFile(existingPath);

  if (!generated) {
    console.error(`❌ Failed to parse generated file: ${generatedPath}`);
    process.exit(1);
  }

  if (!existing) {
    console.error(`❌ Failed to parse existing file: ${existingPath}`);
    process.exit(1);
  }

  console.log(`✅ Parsed generated: ${generated.methods.length} methods, ${generated.properties.length} properties`);
  console.log(`✅ Parsed existing: ${existing.methods.length} methods, ${existing.properties.length} properties`);

  // Analyze merge
  const analysis = analyzeMerge(generated, existing);

  // Print results
  printAnalysis(analysis);

  // Exit code
  process.exit(analysis.conflicts.length > 0 ? 1 : 0);
}

await main();
