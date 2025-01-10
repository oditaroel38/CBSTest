SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[RemittanceUploadReport]
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @tableName NVARCHAR(50);
  DECLARE @SQLString NVARCHAR(MAX) = '';

  SET @tableName = 'dbo.Log_Remittance_' + FORMAT(GETDATE(), 'yyyyMMdd');
  SET @SQLString = 'SELECT 
                     [CONTROLN] AS [CONTROLN],
					 [FullName] AS [FULLNAME],
					 [PNNumber] AS [PN],
					 [DEDAMOUNT] AS [DEDAMOUNT],
					 [SURCHARGE] AS [SURCHARGE],
					 [INTEREST] AS [INTEREST],					
					 [SAVINGS] AS [SAVINGS]
                 FROM ' + @tableName + ' 
                 ';

EXEC sp_executesql @SQLString;
END;




 
