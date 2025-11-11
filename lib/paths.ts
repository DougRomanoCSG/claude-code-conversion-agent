/**
 * Path resolution utilities
 */

import config from "../config.json" with { type: "json" };
import { readdir } from "fs/promises";
import { existsSync } from "fs";

export function resolvePath(relativeFromImportMeta: string, importMetaUrl: string): string {
	const url = new URL(relativeFromImportMeta, importMetaUrl);
	let path = url.pathname;
	// Normalize Windows paths - convert /C:/ to C:/
	if (process.platform === "win32" && path.match(/^\/[A-Z]:/)) {
		path = path.substring(1);
	}
	return path;
}

export function getProjectRoot(importMetaUrl: string): string {
	return resolvePath("../", importMetaUrl);
}

export function getOutputPath(projectRoot: string, entity: string, outputDir?: string): string {
	return outputDir || `${projectRoot}output/${entity}`;
}

/**
 * Get the configured input directory for OnShore source files
 */
export function getInputDirectory(): string {
	return config.inputDirectory;
}

/**
 * Get the full path to a form file
 */
export function getFormPath(entity: string, formType: "Search" | "Detail"): string {
	const inputDir = getInputDirectory();
	const formsPath = config.paths.forms;
	return `${inputDir}/${formsPath}/frm${entity}${formType}.vb`;
}

/**
 * Get the full path to a form designer file
 */
export function getFormDesignerPath(entity: string, formType: "Search" | "Detail"): string {
	const inputDir = getInputDirectory();
	const formsPath = config.paths.forms;
	return `${inputDir}/${formsPath}/frm${entity}${formType}.Designer.vb`;
}

/**
 * Get the full path to a business object file
 */
export function getBusinessObjectPath(entity: string): string {
	const inputDir = getInputDirectory();
	const businessObjectsPath = config.paths.businessObjects;
	return `${inputDir}/${businessObjectsPath}/${entity}Location.vb`;
}

/**
 * Get the full path to a business object base class file
 */
export function getBusinessObjectBasePath(entity: string): string {
	const inputDir = getInputDirectory();
	const basePath = config.paths.businessObjectsBase;
	return `${inputDir}/${basePath}/${entity}LocationBase.vb`;
}

/**
 * Get the full path to a list/search class file
 */
export function getListPath(entity: string): string {
	const inputDir = getInputDirectory();
	const listsPath = config.paths.lists;
	return `${inputDir}/${listsPath}/${entity}LocationSearch.vb`;
}

/**
 * Get the relative path string for use in agent prompts (for Claude Code to search)
 */
export function getFormPathForPrompt(entity: string, formType: "Search" | "Detail"): string {
	const formsPath = config.paths.forms;
	return `${getInputDirectory()}/${formsPath}/frm${entity}${formType}.vb`;
}

export function getBusinessObjectPathForPrompt(entity: string): string {
	const businessObjectsPath = config.paths.businessObjects;
	return `${getInputDirectory()}/${businessObjectsPath}/${entity}Location.vb`;
}

export function getBusinessObjectBasePathForPrompt(entity: string): string {
	const basePath = config.paths.businessObjectsBase;
	return `${getInputDirectory()}/${basePath}/${entity}LocationBase.vb`;
}

export function getListPathForPrompt(entity: string): string {
	const listsPath = config.paths.lists;
	return `${getInputDirectory()}/${listsPath}/${entity}LocationSearch.vb`;
}

/**
 * Get the path to the base Location.vb file
 */
export function getLocationBasePathForPrompt(): string {
	const businessObjectsPath = config.paths.businessObjects;
	return `${getInputDirectory()}/${businessObjectsPath}/Location.vb`;
}

/**
 * Get the configured path to BargeOps.Crewing.API reference project
 */
export function getCrewingApiPath(): string {
	return config.referenceProjects.crewingApi;
}

/**
 * Get the configured path to BargeOps.Crewing.UI reference project
 */
export function getCrewingUiPath(): string {
	return config.referenceProjects.crewingUi;
}

/**
 * Get reference project paths for use in agent prompts
 */
export function getReferenceProjectsForPrompt(): string {
	return `- BargeOps.Crewing.API: ${getCrewingApiPath()}
- BargeOps.Crewing.UI: ${getCrewingUiPath()}`;
}

/**
 * Get the configured path to BargeOps.Admin.API target project
 */
export function getAdminApiPath(): string {
	return config.targetProjects.adminApi;
}

/**
 * Get the configured path to BargeOps.Admin.UI target project
 */
export function getAdminUiPath(): string {
	return config.targetProjects.adminUi;
}

/**
 * Get target project paths for use in agent prompts
 */
export function getTargetProjectsForPrompt(): string {
	return `- BargeOps.Admin.API: ${getAdminApiPath()}
- BargeOps.Admin.UI: ${getAdminUiPath()}`;
}

/**
 * Parse entity name from form name (e.g., "frmFacilitySearch" -> "Facility")
 */
export function parseEntityFromFormName(formName: string): string | null {
	// Remove "frm" prefix and "Search"/"Detail" suffix
	const match = formName.match(/^frm(.+?)(Search|Detail)$/i);
	if (match) {
		return match[1];
	}
	return null;
}

/**
 * Get all available form files in the Forms directory
 */
export async function getAvailableForms(): Promise<string[]> {
	const inputDir = getInputDirectory();
	const formsPath = config.paths.forms;
	const formsDir = `${inputDir}/${formsPath}`;
	
	if (!existsSync(formsDir)) {
		return [];
	}
	
	try {
		const files = await readdir(formsDir);
		// Filter for .vb files that start with "frm" and end with "Search" or "Detail"
		const formFiles = files.filter(file => 
			file.endsWith(".vb") && 
			!file.endsWith(".Designer.vb") &&
			file.match(/^frm.+?(Search|Detail)\.vb$/i)
		);
		
		// Extract form names (without .vb extension)
		return formFiles.map(file => file.replace(/\.vb$/, ""));
	} catch (error) {
		console.error(`Error reading forms directory: ${error}`);
		return [];
	}
}

/**
 * Get the forms directory path
 */
export function getFormsDirectory(): string {
	const inputDir = getInputDirectory();
	const formsPath = config.paths.forms;
	return `${inputDir}/${formsPath}`;
}
