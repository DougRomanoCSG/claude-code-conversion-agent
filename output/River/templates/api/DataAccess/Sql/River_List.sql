-- River_List.sql
-- Get active rivers for dropdown lists (cached)

SELECT
    RiverID,
    Code,
    Name,
    CASE WHEN StartMile IS NOT NULL THEN FORMAT(StartMile, '0.00') ELSE '' END AS StartMile,
    CASE WHEN EndMile IS NOT NULL THEN FORMAT(EndMile, '0.00') ELSE '' END AS EndMile,
    IsLowToHighDirection
FROM River
WHERE IsActive = 1
ORDER BY Code ASC
