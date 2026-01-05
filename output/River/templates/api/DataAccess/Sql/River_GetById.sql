-- River_GetById.sql
-- Retrieve a single river by ID

SELECT
    RiverID,
    Name,
    Code,
    BargeExCode,
    StartMile,
    EndMile,
    IsLowToHighDirection,
    IsActive,
    UpLabel,
    DownLabel
FROM River
WHERE RiverID = @RiverID
