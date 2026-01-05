-- River_Insert.sql
-- Create a new river record

INSERT INTO River (
    Name,
    Code,
    BargeExCode,
    StartMile,
    EndMile,
    IsLowToHighDirection,
    IsActive,
    UpLabel,
    DownLabel
)
OUTPUT INSERTED.RiverID
VALUES (
    @Name,
    @Code,
    @BargeExCode,
    @StartMile,
    @EndMile,
    @IsLowToHighDirection,
    @IsActive,
    @UpLabel,
    @DownLabel
)
