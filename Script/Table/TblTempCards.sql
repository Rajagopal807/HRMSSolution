USE [HRMS_DB]
GO

/****** Object:  Table [dbo].[TblTempCards]    Script Date: 15/04/2026 21:16:56 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TblTempCards](
	[TempCardNo] [varchar](11) NOT NULL,
	[EmpId] [nvarchar](11) NULL,
	[locationcode] [char](6) NULL,
 CONSTRAINT [PK_TblTempCards] PRIMARY KEY CLUSTERED 
(
	[TempCardNo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[TblTempCards]  WITH CHECK ADD  CONSTRAINT [FK_TblTempCards_TblEmpMast] FOREIGN KEY([EmpId])
REFERENCES [dbo].[TblEmpMast] ([EmployeeId])
GO

ALTER TABLE [dbo].[TblTempCards] CHECK CONSTRAINT [FK_TblTempCards_TblEmpMast]
GO


