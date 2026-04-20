USE [HRMS_DB]
GO

/****** Object:  Table [dbo].[TblErrorDisplay]    Script Date: 05/04/2026 12:38:32 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TblErrorDisplay](
	[Empcode] [nvarchar](11) NULL,
	[Tdt] [datetime] NULL,
	[Type] [char](2) NULL,
	[Message] [nvarchar](75) NULL
) ON [PRIMARY]
GO


