SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Remittane_Log](
	[LogId] [int] IDENTITY(1,1) NOT NULL,
	[CONTROLN] [nvarchar](300) NULL,
	[DEDCODE] [nvarchar](30) NULL,
	[DEDAMOUNT] [float] NULL,
	[SCNO] [nvarchar](300) NULL,
	[AFSN] [nvarchar](300) NULL,
	[FullName] [nvarchar](500) NULL,
	[PNNumber] [nvarchar](300) NULL,
	[SurCharge] [float] NULL,
	[Principal] [float] NULL,
	[AmountDeducted] [float] NULL,
	[TransactionType] [int] NULL,
	[ACDIType] [nvarchar](300) NULL
) ON [PRIMARY]
GO


