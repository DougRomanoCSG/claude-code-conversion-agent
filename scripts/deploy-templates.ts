#!/usr/bin/env -S bun run
/**
 * DEPLOY TEMPLATES: Copy generated templates to target MonoRepo projects
 *
 * This script copies templates from output/{Entity}/templates/ to the target projects
 * in the MonoRepo, preserving directory structure and verifying paths.
 *
 * Usage:
 *   bun run scripts/deploy-templates.ts --entity "Vendor"
 *   bun run scripts/deploy-templates.ts --entity "Vendor" --dry-run  (preview only)
 */

import { parsedArgs } from "../lib/flags";
import { getProjectRoot, getAdminApiPath, getAdminUiPath, getSharedProjectPath } from "../lib/paths";
import { existsSync, mkdirSync, readdirSync, statSync, copyFileSync } from "fs";
import { join, dirname, relative } from "path";

const projectRoot = getProjectRoot(import.meta.url);

interface DeployOptions {
	entity: string;
	outputDir?: string;
	dryRun?: boolean;
}

function parseOptions(): DeployOptions {
	const entity = parsedArgs.values.entity as string;
	const outputDir = parsedArgs.values.output as string;
	const dryRun = parsedArgs.values["dry-run"] as boolean;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		console.error("Usage: bun run scripts/deploy-templates.ts --entity \"Vendor\"");
		console.error("       bun run scripts/deploy-templates.ts --entity \"Vendor\" --dry-run");
		process.exit(1);
	}

	return { entity, outputDir, dryRun };
}

function copyDirectory(src: string, dest: string, dryRun: boolean): { files: number; errors: string[] } {
	let filesCopied = 0;
	const errors: string[] = [];

	if (!existsSync(src)) {
		errors.push(`Source directory does not exist: ${src}`);
		return { files: filesCopied, errors };
	}

	function copyRecursive(currentSrc: string, currentDest: string): void {
		const entries = readdirSync(currentSrc, { withFileTypes: true });

		for (const entry of entries) {
			const srcPath = join(currentSrc, entry.name);
			const destPath = join(currentDest, entry.name);

			if (entry.isDirectory()) {
				if (!dryRun) {
					if (!existsSync(destPath)) {
						mkdirSync(destPath, { recursive: true });
					}
				}
				copyRecursive(srcPath, destPath);
			} else {
				if (dryRun) {
					console.log(`  [DRY RUN] Would copy: ${relative(src, srcPath)} → ${relative(dest, destPath)}`);
				} else {
					try {
						// Ensure destination directory exists
						const destDir = dirname(destPath);
						if (!existsSync(destDir)) {
							mkdirSync(destDir, { recursive: true });
						}
						copyFileSync(srcPath, destPath);
						console.log(`  ✓ Copied: ${relative(src, srcPath)}`);
					} catch (error: any) {
						errors.push(`Failed to copy ${srcPath}: ${error.message}`);
					}
				}
				filesCopied++;
			}
		}
	}

	copyRecursive(src, dest);
	return { files: filesCopied, errors };
}

async function deployTemplates(options: DeployOptions): Promise<number> {
	const outputPath = options.outputDir || `${projectRoot}output/${options.entity}`;
	const templatesPath = `${outputPath}/templates`;

	if (!existsSync(templatesPath)) {
		console.error(`\n❌ Error: Templates directory not found: ${templatesPath}`);
		console.error(`\nPlease run the template generator first:`);
		console.error(`   bun run agents/conversion-template-generator.ts --entity "${options.entity}"\n`);
		return 1;
	}

	const sharedPath = getSharedProjectPath();
	const apiPath = getAdminApiPath();
	const uiPath = getAdminUiPath();

	console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║                    TEMPLATE DEPLOYMENT                                     ║
║                                                                            ║
║  Entity: ${options.entity.padEnd(68, " ")}║
║  Source: ${templatesPath.padEnd(67, " ")}║
${options.dryRun ? `║  Mode: DRY RUN (preview only)${" ".padEnd(50, " ")}║\n` : ""}╚════════════════════════════════════════════════════════════════════════════╝
	`);

	// Verify target directories exist
	const targetDirs = [
		{ name: "BargeOps.Shared", path: sharedPath },
		{ name: "BargeOps.Admin.API", path: apiPath },
		{ name: "BargeOps.Admin.UI", path: uiPath },
	];

	for (const dir of targetDirs) {
		if (!existsSync(dir.path)) {
			console.error(`\n❌ Error: Target directory does not exist: ${dir.path}`);
			console.error(`   Please verify your config.json paths are correct.\n`);
			return 1;
		}
	}

	let totalFiles = 0;
	const allErrors: string[] = [];

	// Deploy Shared DTOs
	const sharedTemplates = `${templatesPath}/shared`;
	if (existsSync(sharedTemplates)) {
		console.log(`\n📦 Deploying Shared DTOs to: ${sharedPath}`);
		const sharedDtoPath = `${sharedPath}/Dto`;
		if (!existsSync(sharedDtoPath)) {
			if (!options.dryRun) {
				mkdirSync(sharedDtoPath, { recursive: true });
				console.log(`  ✓ Created directory: Dto/`);
			} else {
				console.log(`  [DRY RUN] Would create directory: Dto/`);
			}
		}
		const result = copyDirectory(sharedTemplates, sharedPath, options.dryRun || false);
		totalFiles += result.files;
		allErrors.push(...result.errors);
	} else {
		console.log(`\n⚠️  No shared templates found at: ${sharedTemplates}`);
	}

	// Deploy API templates
	const apiTemplates = `${templatesPath}/api`;
	if (existsSync(apiTemplates)) {
		console.log(`\n📦 Deploying API templates to: ${apiPath}`);
		
		// Map template subdirectories to API project structure
		const apiMappings = [
			{ src: "Controllers", dest: `${apiPath}/src/Admin.Api/Controllers` },
			{ src: "Repositories", dest: `${apiPath}/src/Admin.Infrastructure/Repositories` },
			{ src: "Services", dest: `${apiPath}/src/Admin.Infrastructure/Services` },
			{ src: "Mapping", dest: `${apiPath}/src/Admin.Infrastructure/Mapping` },
		];

		for (const mapping of apiMappings) {
			const srcPath = `${apiTemplates}/${mapping.src}`;
			if (existsSync(srcPath)) {
				const result = copyDirectory(srcPath, mapping.dest, options.dryRun || false);
				totalFiles += result.files;
				allErrors.push(...result.errors);
			}
		}
	} else {
		console.log(`\n⚠️  No API templates found at: ${apiTemplates}`);
	}

	// Deploy UI templates
	const uiTemplates = `${templatesPath}/ui`;
	if (existsSync(uiTemplates)) {
		console.log(`\n📦 Deploying UI templates to: ${uiPath}`);
		
		// Map template subdirectories to UI project structure
		const uiMappings = [
			{ src: "Controllers", dest: `${uiPath}/Controllers` },
			{ src: "Services", dest: `${uiPath}/Services` },
			{ src: "ViewModels", dest: `${uiPath}/ViewModels` },
			{ src: "Views", dest: `${uiPath}/Views` },
			{ src: "wwwroot", dest: `${uiPath}/wwwroot` },
		];

		for (const mapping of uiMappings) {
			const srcPath = `${uiTemplates}/${mapping.src}`;
			if (existsSync(srcPath)) {
				const result = copyDirectory(srcPath, mapping.dest, options.dryRun || false);
				totalFiles += result.files;
				allErrors.push(...result.errors);
			}
		}
	} else {
		console.log(`\n⚠️  No UI templates found at: ${uiTemplates}`);
	}

	// Summary
	console.log(`\n${"=".repeat(80)}`);
	if (options.dryRun) {
		console.log(`\n✅ DRY RUN Complete: Would deploy ${totalFiles} file(s)`);
		console.log(`\nTo actually deploy, run without --dry-run:`);
		console.log(`   bun run scripts/deploy-templates.ts --entity "${options.entity}"\n`);
	} else {
		if (allErrors.length > 0) {
			console.error(`\n⚠️  Deployment completed with ${allErrors.length} error(s):`);
			allErrors.forEach(err => console.error(`   - ${err}`));
			console.log(`\n✅ Deployed ${totalFiles} file(s) (with errors)\n`);
			return 1;
		} else {
			console.log(`\n✅ Deployment Complete: ${totalFiles} file(s) deployed successfully`);
			console.log(`\n📝 Next Steps:`);
			console.log(`   1. Review deployed files in MonoRepo`);
			console.log(`   2. Verify namespaces match project structure`);
			console.log(`   3. Add DI registration in Startup.cs/Program.cs`);
			console.log(`   4. Update project references if needed\n`);
		}
	}

	return 0;
}

async function main() {
	const options = parseOptions();
	const code = await deployTemplates(options);
	process.exit(code);
}

await main();














