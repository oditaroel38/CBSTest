SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE OR ALTER PROCEDURE [dbo].[RemittanceCheckingRawData]
  @json NVARCHAR(MAX)
AS
 SET NOCOUNT ON;
 BEGIN TRY
      BEGIN TRANSACTION
      
       IF OBJECT_ID('tempdb..#tempRawData') IS NOT NULL DROP TABLE #tempRawData
    
      CREATE TABLE #tempRawData (
     CONTROLN nvarchar(300),
     DEDCODE nvarchar(200),
     DEDAMOUNT float
      );

       INSERT INTO #tempRawData (CONTROLN,DEDCODE,DEDAMOUNT)
      SELECT 
      JSON_VALUE(value, '$.CONTROLN') AS CONTROLN, 
      JSON_VALUE(value, '$.DEDCODE') AS DEDCODE,
      JSON_VALUE(value, '$.DEDAMOUNT') AS DEDAMOUNT
     FROM OPENJSON(@json) AS jsonData;

     SELECT source.CONTROLN, source.DEDCODE, source.DEDAMOUNT, MPI.SCNO,
     CONCAT(MI.FN, ' ', MI.GN, ' ', LEFT(MI.MN, 1), '.') as FullName, MI.AFSN ,MI.BR
     FROM #tempRawData AS source
     INNER JOIN dbo.MEMBERS_PENSIONER_INFO AS MPI WITH (NOLOCK)
     ON MPI.CONTROLN = source.CONTROLN
     INNER JOIN dbo.MEMBERS_INFO AS MI WITH (NOLOCK)
     ON MPI.SCNO = MI.SCNO
   
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
 