namespace BargeOps.Shared.Dto
{
    /// <summary>
    /// DTO for RiverAreaSegment child entity.
    /// Represents a geographic segment (river mile range) within a RiverArea.
    /// </summary>
    public class RiverAreaSegmentDto
    {
        public int RiverAreaSegmentID { get; set; }
        public int RiverAreaID { get; set; }
        public string River { get; set; }  // 3-character river code
        public decimal? StartMile { get; set; }
        public decimal? EndMile { get; set; }
    }
}
