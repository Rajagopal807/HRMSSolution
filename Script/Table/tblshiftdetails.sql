USE [HRMS_DB]
GO

/****** Object:  Table [dbo].[tblshiftdetails]    Script Date: 05/04/2026 12:18:04 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[tblshiftdetails](
	[SHIFTID] [char](2) NOT NULL,
	[dayid] [int] NOT NULL,
	[starttime] [datetime] NULL,
	[endtime] [datetime] NULL,
	[lunchstart] [datetime] NULL,
	[lunchend] [datetime] NULL,
 CONSTRAINT [PK_tblshiftdetails] PRIMARY KEY CLUSTERED 
(
	[SHIFTID] ASC,
	[dayid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[tblshiftdetails]  WITH CHECK ADD  CONSTRAINT [FK_tblshiftdetails_tblshifts] FOREIGN KEY([SHIFTID])
REFERENCES [dbo].[tblshifts] ([SHIFTID])
GO

ALTER TABLE [dbo].[tblshiftdetails] CHECK CONSTRAINT [FK_tblshiftdetails_tblshifts]
GO


