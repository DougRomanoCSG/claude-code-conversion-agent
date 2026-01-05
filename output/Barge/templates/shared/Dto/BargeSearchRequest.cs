namespace BargeOps.Shared.Dto;

/// <summary>
/// Search criteria DTO for Barge search operations
/// Used by both API and UI for filtering barge records
/// </summary>
public class BargeSearchRequest
{
    #region Basic Search Criteria

    /// <summary>
    /// Selected fleet ID for context (from user session)
    /// </summary>
    public int? SelectedFleetID { get; set; }

    /// <summary>
    /// Barge number filter (starts with search)
    /// </summary>
    public string? BargeNum { get; set; }

    /// <summary>
    /// Hull type filter (exact match)
    /// Example: 'B' = Box, 'D' = Deck, 'H' = Hopper, 'O' = Open, 'T' = Tank
    /// </summary>
    public string? HullType { get; set; }

    /// <summary>
    /// Cover type filter (exact match)
    /// Example: 'R' = Roll, 'H' = Hinged, 'OT' = Open Top, 'S' = Sliding
    /// </summary>
    public string? CoverType { get; set; }

    /// <summary>
    /// Operator ID filter (Customer acting as operator)
    /// </summary>
    public int? OperatorID { get; set; }

    /// <summary>
    /// Customer ID filter (Cargo customer)
    /// </summary>
    public int? CustomerID { get; set; }

    /// <summary>
    /// Active only flag (filter by IsActive = true)
    /// Default: true
    /// </summary>
    public bool ActiveOnly { get; set; } = true;

    /// <summary>
    /// Ticket ID filter (exact match)
    /// </summary>
    public int? TicketID { get; set; }

    /// <summary>
    /// Load status filter
    /// Example: 'E' = Empty, 'L' = Loaded, 'P' = Partial
    /// </summary>
    public string? LoadStatus { get; set; }

    /// <summary>
    /// Status filter (barge status code)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Open tickets only flag (filter barges with open tickets)
    /// Default: true
    /// </summary>
    public bool OpenTicketsOnly { get; set; } = true;

    #endregion

    #region Advanced Search Criteria

    /// <summary>
    /// Equipment type filter
    /// Example: 'F' = Fleet-owned, 'C' = Customer-owned, 'O' = Other
    /// </summary>
    public string? EquipmentType { get; set; }

    /// <summary>
    /// USCG number filter (starts with search)
    /// </summary>
    public string? UscgNum { get; set; }

    /// <summary>
    /// Size category filter
    /// Example: 'J' = Jumbo, 'S' = Standard, 'M' = Mini
    /// </summary>
    public string? SizeCategory { get; set; }

    /// <summary>
    /// River filter (for mile range search)
    /// </summary>
    public string? River { get; set; }

    /// <summary>
    /// Start mile for river mile range search
    /// </summary>
    public decimal? StartMile { get; set; }

    /// <summary>
    /// End mile for river mile range search
    /// </summary>
    public decimal? EndMile { get; set; }

    /// <summary>
    /// Contract number filter (starts with search)
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Commodity ID filter
    /// </summary>
    public int? CommodityID { get; set; }

    #endregion

    #region Boat Search Filters

    /// <summary>
    /// Boat search type filter
    /// Options: "In Tow", "Scheduled In", "Scheduled Out"
    /// </summary>
    public string? BoatSearchType { get; set; }

    /// <summary>
    /// Is in tow filter (derived from BoatSearchType)
    /// </summary>
    public bool? IsInTow { get; set; }

    /// <summary>
    /// Is scheduled in filter (derived from BoatSearchType)
    /// </summary>
    public bool? IsScheduledIn { get; set; }

    /// <summary>
    /// Is scheduled out filter (derived from BoatSearchType)
    /// </summary>
    public bool? IsScheduledOut { get; set; }

    /// <summary>
    /// Boat location ID filter (when using boat search)
    /// </summary>
    public int? BoatLocationID { get; set; }

    #endregion

    #region Facility Search Filters

    /// <summary>
    /// Facility search type filter
    /// Options: "At Facility", "Consigned to Facility", "Destination In", "Destination Out", "On Order to Facility"
    /// </summary>
    public string? FacilitySearchType { get; set; }

    /// <summary>
    /// Is at facility filter (derived from FacilitySearchType)
    /// </summary>
    public bool? IsAtFacility { get; set; }

    /// <summary>
    /// Is consigned to facility filter (derived from FacilitySearchType)
    /// </summary>
    public bool? IsConsignedToFacility { get; set; }

    /// <summary>
    /// Is destination in filter (derived from FacilitySearchType)
    /// </summary>
    public bool? IsDestinationIn { get; set; }

    /// <summary>
    /// Is destination out filter (derived from FacilitySearchType)
    /// </summary>
    public bool? IsDestinationOut { get; set; }

    /// <summary>
    /// Is on order to facility filter (derived from FacilitySearchType)
    /// </summary>
    public bool? IsOnOrderToFacility { get; set; }

    /// <summary>
    /// Facility location ID filter (when using facility search)
    /// </summary>
    public int? FacilityLocationID { get; set; }

    #endregion

    #region Ship Search Filters

    /// <summary>
    /// Ship search type filter
    /// Options: "Consigned to Ship", "On Order to Ship"
    /// </summary>
    public string? ShipSearchType { get; set; }

    /// <summary>
    /// Is consigned to ship filter (derived from ShipSearchType)
    /// </summary>
    public bool? IsConsignedToShip { get; set; }

    /// <summary>
    /// Is on order to ship filter (derived from ShipSearchType)
    /// </summary>
    public bool? IsOnOrderToShip { get; set; }

    /// <summary>
    /// Ship location ID filter (when using ship search)
    /// </summary>
    public int? ShipLocationID { get; set; }

    #endregion

    #region DataTables Paging and Sorting

    /// <summary>
    /// Starting index for paging (DataTables parameter)
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// Number of records per page (DataTables parameter)
    /// </summary>
    public int Length { get; set; } = 50;

    /// <summary>
    /// Draw counter for DataTables synchronization (DataTables parameter)
    /// </summary>
    public int Draw { get; set; }

    /// <summary>
    /// Sort column name (DataTables parameter)
    /// </summary>
    public string? SortColumn { get; set; }

    /// <summary>
    /// Sort direction: "asc" or "desc" (DataTables parameter)
    /// </summary>
    public string? SortDirection { get; set; } = "asc";

    #endregion
}
