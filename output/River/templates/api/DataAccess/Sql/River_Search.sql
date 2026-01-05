-- River_Search.sql
-- DataTables server-side processing search with pagination and filtering

DECLARE @FilteredQuery NVARCHAR(MAX);
DECLARE @CountQuery NVARCHAR(MAX);
DECLARE @OrderClause NVARCHAR(MAX);

-- Build dynamic ORDER BY clause
SET @OrderClause = CASE @OrderColumn
    WHEN 'RiverID' THEN 'RiverID'
    WHEN 'Code' THEN 'Code'
    WHEN 'Name' THEN 'Name'
    WHEN 'StartMile' THEN 'StartMile'
    WHEN 'EndMile' THEN 'EndMile'
    WHEN 'IsActive' THEN 'IsActive'
    ELSE 'Code'
END + ' ' + @OrderDirection;

-- Base WHERE clause
DECLARE @WhereClause NVARCHAR(MAX) = ' WHERE 1=1 ';

IF @Code IS NOT NULL AND @Code <> ''
    SET @WhereClause = @WhereClause + ' AND Code LIKE @Code + ''%''';

IF @Name IS NOT NULL AND @Name <> ''
    SET @WhereClause = @WhereClause + ' AND Name LIKE @Name + ''%''';

IF @ActiveOnly = 1
    SET @WhereClause = @WhereClause + ' AND IsActive = 1';

-- Count query
SET @CountQuery = '
SELECT COUNT(*)
FROM River' + @WhereClause;

-- Data query with pagination
SET @FilteredQuery = '
SELECT
    RiverID,
    Name,
    Code,
    BargeExCode,
    CASE WHEN StartMile IS NOT NULL THEN FORMAT(StartMile, ''0.00'') ELSE '''' END AS StartMile,
    CASE WHEN EndMile IS NOT NULL THEN FORMAT(EndMile, ''0.00'') ELSE '''' END AS EndMile,
    IsLowToHighDirection,
    IsActive,
    UpLabel,
    DownLabel
FROM River' + @WhereClause + '
ORDER BY ' + @OrderClause + '
OFFSET @Start ROWS
FETCH NEXT @Length ROWS ONLY';

-- Execute data query
EXEC sp_executesql @FilteredQuery,
    N'@Code NVARCHAR(10), @Name NVARCHAR(100), @Start INT, @Length INT',
    @Code, @Name, @Start, @Length;

-- Execute count query
EXEC sp_executesql @CountQuery,
    N'@Code NVARCHAR(10), @Name NVARCHAR(100)',
    @Code, @Name;
