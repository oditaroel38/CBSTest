SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO 
 
CREATE OR ALTER PROCEDURE [dbo].[RemittanceTransaction]
	 @json NVARCHAR(MAX)
AS
	SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;
 
    -- Drop temporary table if exists
    IF OBJECT_ID('tempdb..#tempPensionData') IS NOT NULL 
        DROP TABLE #tempPensionData;
 
    -- Create dynamic table name
    DECLARE @tableName NVARCHAR(200) = 'Log_Remittance_' + FORMAT(GETDATE(), 'yyyyMMdd');
	DECLARE @LogRemittance NVARCHAR(MAX) = '';
 
    -- Create table if it does not exist
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @tableName)
    BEGIN
        DECLARE @SQLString NVARCHAR(MAX) = '
            CREATE TABLE ' + @tableName + ' (
                LogId INT IDENTITY(1,1) NOT NULL,
                CONTROLN NVARCHAR(300) NULL,
                DEDCODE NVARCHAR(30) NULL,
                DEDAMOUNT FLOAT NULL,
                SCNO NVARCHAR(300) NULL,
                AFSN NVARCHAR(300) NULL,
                FullName NVARCHAR(500) NULL,
                PNNumber NVARCHAR(300) NULL,
                SurCharge FLOAT NULL,
                Interest FLOAT NULL,
                Bal FLOAT NULL,
                Savings FLOAT NULL,
                TransactionType INT NULL,
                ACDIType NVARCHAR(300) NULL
            )';
        EXEC (@SQLString);
    END
 
    -- Create temporary table for saving data
    IF OBJECT_ID('tempdb..#tempSavingData') IS NOT NULL 
        DROP TABLE #tempSavingData;
    CREATE TABLE #tempSavingData (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CONTROLN NVARCHAR(200),
        DEDCODE NVARCHAR(200),
        DEDAMOUNT FLOAT,
        SCNO NVARCHAR(200),
        FullName NVARCHAR(200),
        AFSN NVARCHAR(200),
        BR DECIMAL,
        PN NVARCHAR(200),
        SURBALAMT FLOAT,
        SURBAL FLOAT,
        INTBALAMT FLOAT,
        INTBAL FLOAT,
		PRNAMT FLOAT,
        BAL FLOAT,
        SAVINGS FLOAT,
        ACDIType INT
    );
 
    -- Insert data into temporary table
    INSERT INTO #tempSavingData
    SELECT 
        JSON_VALUE(value, '$.CONTROLN'),
        JSON_VALUE(value, '$.DEDCODE'),
        JSON_VALUE(value, '$.DEDAMOUNT'),
        JSON_VALUE(value, '$.SCNO'),
        JSON_VALUE(value, '$.FullName'),
        JSON_VALUE(value, '$.AFSN'),
        JSON_VALUE(value, '$.BR'),
        JSON_VALUE(value, '$.PN'),
        JSON_VALUE(value, '$.SURBALAMT'),
        JSON_VALUE(value, '$.SURBAL'),
        JSON_VALUE(value, '$.INTBALAMT'),
        JSON_VALUE(value, '$.INTBAL'),
		JSON_VALUE(value, '$.PRNAMT'),
        JSON_VALUE(value, '$.BAL'),
        JSON_VALUE(value, '$.SAVINGS'),
        JSON_VALUE(value, '$.ACDIType')
    FROM OPENJSON(@json) AS jsonData;
 
    -- Insert data into LOAN_LED
    INSERT INTO dbo.LOAN_LED (SCNO, PN, TDATE, SURAMT, SURBAL, INTAMT, INTBAL, PRNAMT, BAL, TAMT, TCODE, UCODE, VCODE, TCTR, OR_, DTSYNC, BR)
    SELECT 
        t.SCNO, 
        t.PN, 
        GETDATE(), 
        t.SURBALAMT, 
        t.SURBAL, 
        t.INTBALAMT, 
        t.INTBAL, 
        -t.PRNAMT, 
        t.BAL,
        t.DEDAMOUNT, 
        'P', 
        ld.UCODE, 
        ld.VCODE,
        lr.TCTR + 1, 
        ld.OR_, 
        ld.DTSYNC,  
        ld.BR
    FROM #tempSavingData t
    LEFT JOIN dbo.LOAN_REG lr ON t.SCNO = lr.SCNO AND t.DEDCODE = lr.DEDTYPE
    LEFT JOIN dbo.LOAN_LED ld ON t.SCNO = ld.SCNO AND lr.PN = ld.PN AND lr.TCTR = ld.TCTR;
 
    -- Update LOAN_REG
    UPDATE lr
    SET
        SURBAL = t.SURBAL,
        INTBAL = t.INTBAL,
        TDATE = GETDATE(),
        BAL = t.BAL,
        TCTR = ld.TCTR + 1
    FROM #tempSavingData t
    JOIN dbo.LOAN_REG lr ON t.SCNO = lr.SCNO AND t.DEDCODE = lr.DEDTYPE
    JOIN dbo.LOAN_LED ld ON t.SCNO = ld.SCNO AND lr.PN = ld.PN AND lr.TCTR = ld.TCTR;
 
    -- Insert into SD_LED
    INSERT INTO dbo.SD_LED (SCNO, TCTR, TDATE, TAMT, RBAL, WBAL, TCODE, CHKNUM, VCODE, UCODE, JSC, PBNO, BR)
    SELECT 
        t.SCNO, 
        SD.TCTR + 1, 
        GETDATE(), 
        t.SAVINGS, 
        (MB.SDRBAL + t.SAVINGS), 
        (MB.SDWBAL + t.SAVINGS), 
        SD.TCODE,
        SD.CHKNUM, 
        SD.VCODE, 
        SD.UCODE, 
        SD.JSC, 
        SD.PBNO, 
        SD.BR
    FROM #tempSavingData t
    INNER JOIN dbo.MEMBERS_BALANCES MB ON t.SCNO = MB.SCNO 
    INNER JOIN dbo.SD_LED SD ON t.SCNO = SD.SCNO AND MB.SDCTR = SD.TCTR
    WHERE t.SAVINGS > 0;
 
    -- Update MEMBERS_BALANCES
    UPDATE MB
    SET 
        SDRBAL = MB.SDRBAL + t.SAVINGS,
        SDWBAL = MB.SDWBAL + t.SAVINGS,
        SDCTR = SD.TCTR + 1, 
        SDTRDATE = GETDATE()
    FROM #tempSavingData t
    INNER JOIN dbo.MEMBERS_BALANCES MB ON t.SCNO = MB.SCNO 
    INNER JOIN dbo.SD_LED SD ON t.SCNO = SD.SCNO AND MB.SDCTR = SD.TCTR
    WHERE t.SAVINGS > 0;

	SET @LogRemittance = 'INSERT INTO '+ @tableName +' (CONTROLN, DEDCODE, DEDAMOUNT, SCNO, AFSN, FullName, PNNumber, SurCharge, Interest, Bal, Savings, TransactionType, ACDIType)' +' '
                          +'SELECT CONTROLN,DEDCODE,DEDAMOUNT,SCNO,AFSN,FullName,PN,SURBALAMT,INTBALAMT,BAL,SAVINGS,1,ACDIType FROM #tempSavingData';
    
	EXEC (@LogRemittance);
 
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF (@@TRANCOUNT > 0)
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'Error detected, all changes reversed.';
    END 
    SELECT
        ERROR_NUMBER() AS ErrorNumber,
        ERROR_STATE() AS ErrorState,
        ERROR_SEVERITY() AS ErrorSeverity,
        ERROR_PROCEDURE() AS ErrorProcedure,
        ERROR_LINE() AS ErrorLine,
        ERROR_MESSAGE() AS ErrorMessage;
END CATCH;