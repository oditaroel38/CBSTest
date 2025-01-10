CREATE OR ALTER PROCEDURE [dbo].[RemittanceComputingRawData]
  @json NVARCHAR(MAX)
AS
 SET NOCOUNT ON;
 BEGIN TRY
      BEGIN TRANSACTION
      
    -- Drop the temporary table if it exists
    IF OBJECT_ID('tempdb..#tempComputingData') IS NOT NULL DROP TABLE #tempComputingData;
    -- Create the temporary table
    CREATE TABLE #tempComputingData (
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
    -- Insert data into the temporary table
    INSERT INTO #tempComputingData (CONTROLN, DEDCODE, DEDAMOUNT, SCNO, FullName, AFSN, BR)
    SELECT 
        JSON_VALUE(value, '$.CONTROLN'),
        JSON_VALUE(value, '$.DEDCODE'),
        JSON_VALUE(value, '$.DEDAMOUNT'),
        JSON_VALUE(value, '$.SCNO'),
        JSON_VALUE(value, '$.FullName'),
        JSON_VALUE(value, '$.AFSN'),
        JSON_VALUE(value, '$.BR')
    FROM OPENJSON(@json) AS jsonData;
    -- Use a single query to join and update values
    UPDATE t
    SET 
  SURBALAMT = IIF(t.DEDAMOUNT < ld.SURAMT, -t.DEDAMOUNT, COALESCE(-ld.SURAMT, 0) ) ,
        SURBAL = IIF(t.DEDAMOUNT < ld.SURAMT, (ld.SURBAL - t.DEDAMOUNT), COALESCE(-ld.SURBAL, 0) ) ,
  INTBALAMT = CASE WHEN ld.SURAMT > t.DEDAMOUNT
       THEN ld.INTAMT
       WHEN ld.INTAMT > t.DEDAMOUNT
       THEN -t.DEDAMOUNT
      ELSE -ld.INTAMT
     END,
  INTBAL =  CASE WHEN ld.SURAMT > t.DEDAMOUNT
       THEN ld.INTBAL
       WHEN ld.INTAMT > t.DEDAMOUNT
       THEN ld.INTBAL -t.DEDAMOUNT
      ELSE -ld.INTBAL
     END,
  PRNAMT = CASE WHEN ((COALESCE(ld.SURBAL,0) + COALESCE(ld.INTBAL, 0) + COALESCE(ld.BAL, 0)) >= DEDAMOUNT)
     THEN 
      CASE WHEN ((COALESCE(ld.SURBAL,0) + COALESCE(ld.INTBAL, 0)) <= t.DEDAMOUNT)
       THEN ROUND(DEDAMOUNT - COALESCE(ld.SURBAL + ld.INTBAL,0), 2)
      ELSE 0
      END
                    ELSE 0
                    END,
        BAL = lr.BAL,
        PN = lr.PN
    FROM #tempComputingData t
    INNER JOIN [dbo].[LOAN_REG] lr ON t.SCNO = lr.SCNO AND t.DEDCODE = lr.DEDTYPE
    INNER JOIN [dbo].[LOAN_LED] ld ON t.SCNO = ld.SCNO AND lr.PN = ld.PN AND lr.TCTR = ld.TCTR;
 UPDATE #tempComputingData 
  SET 
   SURBAL = IIF(PRNAMT > 0, 0, SURBAL),
   INTBAL = IIF(PRNAMT > 0, 0, INTBAL),
   BAL = IIF(PRNAMT > 0, BAL - PRNAMT, BAL)
    UPDATE #tempComputingData 
  SET SAVINGS =  CASE WHEN ((COALESCE(SURBAL,0) + COALESCE(INTBAL, 0) + COALESCE(BAL, 0)) < DEDAMOUNT)
      THEN DEDAMOUNT
        ELSE 0
                    END
  WHERE BAL <= 0
    -- Select the results from the temporary table
    SELECT * FROM #tempComputingData;
   
  COMMIT TRANSACTION
 END TRY
 BEGIN CATCH
  IF (@@TRANCOUNT > 0)
     BEGIN
     ROLLBACK TRANSACTION
     PRINT 'Error detected, all changes reversed.'
     END 
  SELECT
   ERROR_NUMBER() AS ErrorNumber,
   ERROR_STATE() AS ErrorState,
   ERROR_SEVERITY() AS ErrorSeverity,
   ERROR_PROCEDURE() AS ErrorProcedure,
   ERROR_LINE() AS ErrorLine,
   ERROR_MESSAGE() AS ErrorMessage
 END CATCH
 