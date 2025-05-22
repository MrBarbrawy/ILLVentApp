-- Fix issues with Medical History tables

-- First ensure Family History relationships are nullable
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FamilyHistories') AND name = 'CancerPolypsRelationship' AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN CancerPolypsRelationship nvarchar(MAX) NULL;
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN AnemiaRelationship nvarchar(MAX) NULL;
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN DiabetesRelationship nvarchar(MAX) NULL;
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN BloodClotsRelationship nvarchar(MAX) NULL;
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN HeartDiseaseRelationship nvarchar(MAX) NULL;
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN StrokeRelationship nvarchar(MAX) NULL;
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN HighBloodPressureRelationship nvarchar(MAX) NULL;
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN AnesthesiaReactionRelationship nvarchar(MAX) NULL;
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN BleedingProblemsRelationship nvarchar(MAX) NULL;
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN HepatitisRelationship nvarchar(MAX) NULL;
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN OtherConditionDetails nvarchar(MAX) NULL;
    ALTER TABLE dbo.FamilyHistories ALTER COLUMN OtherConditionRelationship nvarchar(MAX) NULL;
    
    PRINT 'FamilyHistories columns updated to allow NULL values';
END
ELSE
BEGIN
    PRINT 'FamilyHistories columns already allow NULL values';
END

-- Fix cascade delete behavior for related tables
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SocialHistories_MedicalHistories_MedicalHistoryId')
BEGIN
    ALTER TABLE dbo.SocialHistories DROP CONSTRAINT FK_SocialHistories_MedicalHistories_MedicalHistoryId;
    ALTER TABLE dbo.SocialHistories ADD CONSTRAINT FK_SocialHistories_MedicalHistories_MedicalHistoryId 
    FOREIGN KEY (MedicalHistoryId) REFERENCES dbo.MedicalHistories(MedicalHistoryId) ON DELETE CASCADE;
    
    PRINT 'SocialHistories cascade delete constraint updated';
END
ELSE
BEGIN
    PRINT 'SocialHistories FK constraint not found, no changes made';
END

-- Increase QrCode column size in MedicalHistories table
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MedicalHistories') AND name = 'QrCode')
BEGIN
    -- Check if the column needs to be enlarged
    DECLARE @column_length int
    SELECT @column_length = max_length/2 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.MedicalHistories') AND name = 'QrCode'
    
    IF @column_length < 4000
    BEGIN
        ALTER TABLE dbo.MedicalHistories ALTER COLUMN QrCode nvarchar(MAX);
        PRINT 'QrCode column updated to nvarchar(MAX)';
    END
    ELSE
    BEGIN
        PRINT 'QrCode column already large enough';
    END
END

-- Add DiabetesType nullable to medical histories if needed
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MedicalHistories') AND name = 'DiabetesType' AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.MedicalHistories ALTER COLUMN DiabetesType nvarchar(20) NULL;
    PRINT 'DiabetesType column updated to allow NULL values';
END
ELSE
BEGIN
    PRINT 'DiabetesType column already allows NULL values';
END

-- Add AllergiesDetails nullable to medical histories if needed
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MedicalHistories') AND name = 'AllergiesDetails' AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.MedicalHistories ALTER COLUMN AllergiesDetails nvarchar(500) NULL;
    PRINT 'AllergiesDetails column updated to allow NULL values';
END
ELSE
BEGIN
    PRINT 'AllergiesDetails column already allows NULL values';
END

-- Add BirthControlMethod nullable to medical histories if needed
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MedicalHistories') AND name = 'BirthControlMethod' AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.MedicalHistories ALTER COLUMN BirthControlMethod nvarchar(100) NULL;
    PRINT 'BirthControlMethod column updated to allow NULL values';
END
ELSE
BEGIN
    PRINT 'BirthControlMethod column already allows NULL values';
END

-- Add missing indexes if needed
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalHistories_UserId' AND object_id = OBJECT_ID('dbo.MedicalHistories'))
BEGIN
    CREATE INDEX IX_MedicalHistories_UserId ON dbo.MedicalHistories(UserId);
    PRINT 'Added index on MedicalHistories.UserId';
END
ELSE
BEGIN
    PRINT 'Index on MedicalHistories.UserId already exists';
END

PRINT 'Database fix script completed'; 