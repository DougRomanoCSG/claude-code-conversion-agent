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
import { existsSync, mkdirSync, readdirSync, statSync, copyFileSync, readFileSync, writeFileSync } from "fs";
import { join, dirname, relative } from "path";

const projectRoot = getProjectRoot(import.meta.url);

interface DeployOptions {
	entity: string;
	outputDir?: string;
	dryRun?: boolean;
	skipExisting?: boolean;
}

function parseOptions(): DeployOptions {
	const entity = parsedArgs.values.entity as string;
	const outputDir = parsedArgs.values.output as string;
	const dryRun = parsedArgs.values["dry-run"] as boolean;
	const skipExisting = (parsedArgs.values["skip-existing"] as boolean) || false;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		console.error("Usage: bun run scripts/deploy-templates.ts --entity \"Vendor\"");
		console.error("       bun run scripts/deploy-templates.ts --entity \"Vendor\" --dry-run");
		console.error("       bun run scripts/deploy-templates.ts --entity \"Vendor\" --skip-existing");
		process.exit(1);
	}

	return { entity, outputDir, dryRun, skipExisting };
}

type CopyOverrides = {
	/**
	 * Override destination root for a given relative path (within src).
	 * Return null to use the default dest root.
	 */
	resolveDestRoot?: (relativePath: string) => string | null;
	/**
	 * Optionally transform file contents before writing to destination.
	 * Only called for text files we read (currently .cs).
	 */
	transformText?: (args: { relativePath: string; srcPath: string; destPath: string; contents: string }) => string;
};

function copyDirectory(
	src: string,
	dest: string,
	dryRun: boolean,
	overrides: CopyOverrides = {},
	skipExisting: boolean = false,
): { files: number; errors: string[] } {
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
			const relFromRoot = relative(src, srcPath);
			const destRootOverride = overrides.resolveDestRoot ? overrides.resolveDestRoot(relFromRoot) : null;
			const effectiveDestRoot = destRootOverride ?? dest;
			const destPath = join(effectiveDestRoot, relFromRoot);

			if (entry.isDirectory()) {
				if (!dryRun) {
					if (!existsSync(destPath)) {
						mkdirSync(destPath, { recursive: true });
					}
				}
				// Keep traversing using the original src tree; destPath already includes relFromRoot
				copyRecursive(srcPath, destPath);
			} else {
				if (dryRun) {
					if (skipExisting && existsSync(destPath)) {
						console.log(`  [DRY RUN] Would skip (exists): ${relFromRoot}`);
					} else {
						console.log(`  [DRY RUN] Would copy: ${relFromRoot} → ${relative(dest, destPath)}`);
					}
				} else {
					try {
						if (skipExisting && existsSync(destPath)) {
							console.log(`  ↷ Skipped (exists): ${relFromRoot}`);
							continue;
						}

						// Ensure destination directory exists
						const destDir = dirname(destPath);
						if (!existsSync(destDir)) {
							mkdirSync(destDir, { recursive: true });
						}

						// Apply optional text transforms for C# files when requested.
						if (srcPath.toLowerCase().endsWith(".cs") && overrides.transformText) {
							const contents = readFileSync(srcPath, "utf8");
							const transformed = overrides.transformText({ relativePath: relFromRoot, srcPath, destPath, contents });
							writeFileSync(destPath, transformed, "utf8");
						} else {
							copyFileSync(srcPath, destPath);
						}

						console.log(`  ✓ Copied: ${relFromRoot}`);
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
		console.error(`   bun run generate-template-api --entity "${options.entity}"`);
		console.error(`   bun run generate-template-ui --entity "${options.entity}"`);
		console.error(`   bun run generate-templates --entity "${options.entity}"\n`);
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
		const result = copyDirectory(sharedTemplates, sharedPath, options.dryRun || false, {}, options.skipExisting || false);
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
			// Enhancements: support additional generated folders
			{ src: "Sql", dest: `${apiPath}/src/Admin.Infrastructure/DataAccess/Sql/${options.entity}` },
			{ src: "Validation", dest: `${apiPath}/src/Admin.Infrastructure/Validators` },
			{ src: "Utilities", dest: `${apiPath}/src/Admin.Infrastructure/Utilities` },
		];

		for (const mapping of apiMappings) {
			const srcPath = `${apiTemplates}/${mapping.src}`;
			if (existsSync(srcPath)) {
				if (mapping.src === "Repositories") {
					// Route repository interfaces (I*Repository.cs) to Admin.Infrastructure/Abstractions to match MonoRepo conventions.
					const abstractionsPath = `${apiPath}/src/Admin.Infrastructure/Abstractions`;
					const result = copyDirectory(srcPath, mapping.dest, options.dryRun || false, {
						resolveDestRoot: (relPath) => {
							const fileName = relPath.replace(/\\/g, "/").split("/").pop() ?? relPath;
							if (/^I[A-Z].*Repository\.cs$/i.test(fileName)) return abstractionsPath;
							return null;
						},
						transformText: ({ relativePath, destPath, contents }) => {
							const fileName = relativePath.replace(/\\/g, "/").split("/").pop() ?? relativePath;
							const movedToAbstractions =
								/^I[A-Z].*Repository\.cs$/i.test(fileName) && destPath.includes(`${apiPath}/src/Admin.Infrastructure/Abstractions`);

							if (!movedToAbstractions) return contents;

							// Normalize namespace for interfaces moved into Abstractions.
							// (Some generators output namespace Admin.Infrastructure.Repositories for interfaces.)
							return contents.replace(/namespace\s+Admin\.Infrastructure\.Repositories\s*;/g, "namespace Admin.Infrastructure.Abstractions;");
						},
					}, options.skipExisting || false);
					totalFiles += result.files;
					allErrors.push(...result.errors);
					continue;
				}

				if (mapping.src === "Validation") {
					// Normalize namespace to match Admin.Infrastructure.Validators used in MonoRepo.
					const result = copyDirectory(srcPath, mapping.dest, options.dryRun || false, {
						transformText: ({ contents }) =>
							contents.replace(/namespace\s+Admin\.Infrastructure\.Validation\s*;/g, "namespace Admin.Infrastructure.Validators;"),
					}, options.skipExisting || false);
					totalFiles += result.files;
					allErrors.push(...result.errors);
					continue;
				}

				const result = copyDirectory(srcPath, mapping.dest, options.dryRun || false, {}, options.skipExisting || false);
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
				const result = copyDirectory(srcPath, mapping.dest, options.dryRun || false, {}, options.skipExisting || false);
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














