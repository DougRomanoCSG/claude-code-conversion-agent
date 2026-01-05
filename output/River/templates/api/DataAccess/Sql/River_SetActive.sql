-- River_SetActive.sql
-- Soft delete or activate a river (set IsActive flag)
-- CRITICAL: DO NOT use hard delete - use soft delete pattern

UPDATE River
SET IsActive = @IsActive
WHERE RiverID = @RiverID
