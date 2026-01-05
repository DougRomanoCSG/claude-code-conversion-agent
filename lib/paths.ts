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

export function getFormDesignerPathForPrompt(entity: string, formType: "Search" | "Detail"): string {
	const formsPath = config.paths.forms;
	return `${getInputDirectory()}/${formsPath}/frm${entity}${formType}.Designer.vb`;
}

/**
 * Get the full path to a form file by form name (e.g., frmBargePositions)
 */
export function getFormPathByNameForPrompt(formName: string): string {
	const formsPath = config.paths.forms;
	return `${getInputDirectory()}/${formsPath}/${formName}.vb`;
}

/**
 * Get the full path to a form designer file by form name (e.g., frmBargePositions.Designer.vb)
 */
export function getFormDesignerPathByNameForPrompt(formName: string): string {
	const formsPath = config.paths.forms;
	return `${getInputDirectory()}/${formsPath}/${formName}.Designer.vb`;
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
 * Get the configured path to BargeOps.Shared project
 */
export function getSharedProjectPath(): string {
	return config.targetProjects.shared;
}

/**
 * Get target project paths for use in agent prompts
 */
export function getTargetProjectsForPrompt(): string {
	return `- BargeOps.Shared: ${getSharedProjectPath()} (DTOs and Models)
- BargeOps.Admin.API: ${getAdminApiPath()}
- BargeOps.Admin.UI: ${getAdminUiPath()}`;
}

/**
 * Parse entity name from form name (e.g., "frmFacilitySearch" -> "Facility")
 */
export function parseEntityFromFormName(formName: unknown): string | null {
	if (typeof formName !== "string" || !formName) {
		return null;
	}
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
 * Detect child forms opened by a parent form by analyzing the VB code
 */
export async function detectChildForms(formName: string): Promise<string[]> {
	const inputDir = getInputDirectory();
	const formsPath = config.paths.forms;
	const formFilePath = `${inputDir}/${formsPath}/${formName}.vb`;

	if (!existsSync(formFilePath)) {
		return [];
	}

	try {
		const file = Bun.file(formFilePath);
		const content = await file.text();

		const childForms = new Set<string>();

		// Pattern 1: Dim frm As New frmChildForm() or similar
		const newFormPattern = /Dim\s+\w+\s+As\s+New\s+(frm\w+)/gi;
		let match;
		while ((match = newFormPattern.exec(content)) !== null) {
			childForms.add(match[1]);
		}

		// Pattern 2: frmChildForm.Show() or frmChildForm.ShowDialog()
		const showFormPattern = /(frm\w+)\.(Show|ShowDialog)\s*\(/gi;
		while ((match = showFormPattern.exec(content)) !== null) {
			childForms.add(match[1]);
		}

		// Pattern 3: Dim frm As frmChildForm = New frmChildForm()
		const dimAsFormPattern = /Dim\s+\w+\s+As\s+(frm\w+)\s*=/gi;
		while ((match = dimAsFormPattern.exec(content)) !== null) {
			childForms.add(match[1]);
		}

		// Pattern 4: Direct instantiation: New frmChildForm()
		const directNewPattern = /New\s+(frm\w+)\s*\(/gi;
		while ((match = directNewPattern.exec(content)) !== null) {
			childForms.add(match[1]);
		}

		// Filter out the parent form itself
		return Array.from(childForms).filter(f => f !== formName);
	} catch (error) {
		console.error(`Error detecting child forms for ${formName}: ${error}`);
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

/**
 * Get example paths for BargeOps.Crewing.API
 */
export function getCrewingApiExamples(): {
	controllers: string;
	domainModels: string;
	dtos: string;
	repositories: string;
	services: string;
	mappings: string;
} {
	const basePath = getCrewingApiPath();
	return {
		controllers: `${basePath}/src/Crewing.Api/Controllers`,
		domainModels: `${basePath}/src/Crewing.Domain/Models`,
		dtos: `${basePath}/src/Crewing.Domain/Dto`,
		repositories: `${basePath}/src/Crewing.Infrastructure/Repositories`,
		services: `${basePath}/src/Crewing.Domain/Services`,
		mappings: `${basePath}/src/Crewing.Infrastructure/Mappings`,
	};
}

/**
 * Get example paths for BargeOps.Crewing.UI
 */
export function getCrewingUiExamples(): {
	controllers: string;
	viewModels: string;
	views: string;
	javascript: string;
	css: string;
	services: string;
} {
	const basePath = getCrewingUiPath();
	return {
		controllers: `${basePath}/Controllers`,
		viewModels: `${basePath}/Models`,
		views: `${basePath}/Views`,
		javascript: `${basePath}/wwwroot/js`,
		css: `${basePath}/wwwroot/css`,
		services: `${basePath}/Services`,
	};
}

/**
 * Get example paths for BargeOps.Shared (shared DTOs - the ONLY data models)
 */
export function getSharedExamples(): {
	dtos: string;
	constants: string;
} {
	const basePath = getSharedProjectPath();
	return {
		dtos: `${basePath}/Dto`,
		constants: `${basePath}/Constants`,
	};
}

/**
 * Get example paths for BargeOps.Admin.API (target patterns)
 * NOTE: DTOs and Models are now in BargeOps.Shared, not in Admin.API
 */
export function getAdminApiExamples(): {
	controllers: string;
	repositories: string;
	services: string;
	mappings: string;
	infrastructure: string;
} {
	const basePath = getAdminApiPath();
	return {
		controllers: `${basePath}/src/Admin.Api/Controllers`,
		repositories: `${basePath}/src/Admin.Infrastructure/Repositories`,
		services: `${basePath}/src/Admin.Infrastructure/Services`,
		mappings: `${basePath}/src/Admin.Infrastructure/Mapping`,
		infrastructure: `${basePath}/src/Admin.Infrastructure`,
	};
}

/**
 * Get example paths for BargeOps.Admin.UI (target patterns)
 */
export function getAdminUiExamples(): {
	controllers: string;
	viewModels: string;
	views: string;
	javascript: string;
	css: string;
	services: string;
} {
	const basePath = getAdminUiPath();
	return {
		controllers: `${basePath}/Controllers`,
		viewModels: `${basePath}/ViewModels`,
		views: `${basePath}/Views`,
		javascript: `${basePath}/wwwroot/js`,
		css: `${basePath}/wwwroot/css`,
		services: `${basePath}/Services`,
	};
}

/**
 * Get detailed reference examples guide for agent prompts (concise version)
 */
export function getDetailedReferenceExamples(): string {
	const shared = getSharedExamples();
	const crewingApi = getCrewingApiExamples();
	const crewingUi = getCrewingUiExamples();
	const adminApi = getAdminApiExamples();
	const adminUi = getAdminUiExamples();

	return `REFERENCE EXAMPLES:

⭐ BargeOps.Shared (${getSharedProjectPath()}):
CRITICAL: DTOs are the ONLY data models - no separate domain models!
- DTOs: ${shared.dtos} (FacilityDto.cs, BoatLocationDto.cs, LookupItemDto.cs)
  Example: ${shared.dtos}/FacilityDto.cs - Complete entity DTO with [Sortable]/[Filterable] attributes
  Example: ${shared.dtos}/BoatLocationDto.cs - Full DTO used by both API and UI
  Example: ${shared.dtos}/FacilitySearchRequest.cs - Search criteria DTO
  Example: ${shared.dtos}/PagedResult.cs, DataTableResponse.cs - Generic paging wrappers
  ⭐ These DTOs are used DIRECTLY by both API and UI - no mapping needed!

BargeOps.Crewing.API (${getCrewingApiPath()}):
- Controllers: ${crewingApi.controllers} (CrewingController.cs, BoatController.cs)
- Models: ${crewingApi.domainModels} (Crewing.cs, Boat.cs)
- DTOs: ${crewingApi.dtos} (CrewingDto.cs, CrewingSearchRequest.cs)
- Repos: ${crewingApi.repositories} (ICrewingRepository.cs, CrewingRepository.cs)
- Services: ${crewingApi.services} (ICrewingService.cs, CrewingService.cs)
- Mappings: ${crewingApi.mappings} (CrewingMappingProfile.cs)

BargeOps.Crewing.UI (${getCrewingUiPath()}):
- Controllers: ${crewingUi.controllers} (CrewingSearchController.cs, BoatSearchController.cs)
- ViewModels: ${crewingUi.viewModels} (CrewingSearchViewModel.cs, CrewingEditViewModel.cs)
- Views: ${crewingUi.views} (CrewingSearch/Index.cshtml, Edit.cshtml)
- JS: ${crewingUi.javascript} (crewingSearch.js - DataTables patterns)
- CSS: ${crewingUi.css} (crewingSearch.css)
- Services: ${crewingUi.services} (ICrewingService.cs, CrewingService.cs)

⭐ BargeOps.Admin.API Target (${getAdminApiPath()}):
PRIMARY REFERENCE: FacilityController.cs and BoatLocationController.cs
NOTE: DTOs are in BargeOps.Shared - API uses them directly!
- Controllers: ${adminApi.controllers}/FacilityController.cs, BoatLocationController.cs (canonical API patterns)
- Repos: ${adminApi.repositories} (IFacilityRepository.cs, FacilityRepository.cs - Dapper patterns)
  Example: FacilityRepository.cs - Returns DTOs directly from stored procedures
  Example: BoatLocationRepository.cs - Complete CRUD with DTOs
- Services: ${adminApi.services} (IFacilityService.cs, FacilityService.cs)
  Example: FacilityService.cs - Service layer using DTOs (no mapping needed!)
- Mappings: ${adminApi.mappings} (Mapping profiles - usually NOT needed with DTOs)

⭐ BargeOps.Admin.UI Target (${getAdminUiPath()}):
PRIMARY REFERENCE: BoatLocationSearchController.cs
- Controllers: ${adminUi.controllers}/BoatLocationSearchController.cs (canonical Admin UI pattern)
- ViewModels: ${adminUi.viewModels} (BoatLocationSearchViewModel.cs, BoatLocationEditViewModel.cs)
- Views: ${adminUi.views}/BoatLocationSearch/ (Index.cshtml, Edit.cshtml)
- JS: ${adminUi.javascript}/boatLocationSearch.js (DataTables initialization)
- Services: ${adminUi.services} (IBoatLocationService.cs, BoatLocationService.cs - API client)`;
}
