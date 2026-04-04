USE [HRMS_DB]
GO
/****** Object:  StoredProcedure [dbo].[CreateMusterServiceProc]    Script Date: 04/04/2026 15:56:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Rajagopal
-- Create date: 04-Apr-2026
-- Description:	Procedure to create Muster for employees
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[CreateMusterServiceProc]
	-- Add the parameters for the stored procedure here
	@fromDate as DateTime, 
	@empid varchar(11)
AS
Declare @MFDate varchar(11), @MTDate varchar(11), @CreateMusterFor bit, @mEmpid varchar(11)
BEGIN
	set @MFDate = CONVERT(varchar(10),@fromDate,101)
	if @empid= ''
	BEGIN
		Declare emp_fetch Scroll Cursor For SELECT EmployeeID FROM Tblempmast WHERE IsActive=1
		Open emp_fetch
		Fetch First FROM emp_fetch Into @mEmpid
		while @@FETCH_STATUS =0
		BEGIN 

		exec CreateMusterFor @mEmpid,@MFDate,1,@CreateMusterFor output

		Fetch Next From emp_fetch Into @mEmpid
	END
	close emp_fetch
	Deallocate emp_fetch
	END

	if @Empid<>''
	BEGIN
	  exec CreateMusterFor @empid,@MFDate,1,@CreateMusterFor output
	END

END

--EXEC CreateMusterServiceProc '2026-03-01', '00000011255'
--EXEC CreateMusterFor '00000011255','2026-03-01',1,0