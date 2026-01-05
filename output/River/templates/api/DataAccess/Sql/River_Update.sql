-- River_Update.sql
-- Update an existing river record

UPDATE River
SET
    Name = @Name,
    Code = @Code,
    BargeExCode = @BargeExCode,
    StartMile = @StartMile,
    EndMile = @EndMile,
    IsLowToHighDirection = @IsLowToHighDirection,
    IsActive = @IsActive,
    UpLabel = @UpLabel,
    DownLabel = @DownLabel
WHERE RiverID = @RiverID
