-- River_GetAll.sql
-- Retrieve all rivers

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
ORDER BY Code ASC
