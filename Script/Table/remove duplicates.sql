WITH CTE AS (
    SELECT *,
           ROW_NUMBER() OVER (
               PARTITION BY EmployeeId, FromDate, Session
               ORDER BY ApplicationId
           ) AS rn
    FROM TblLeaveApplications
    WHERE IsDeleted = 0
)
DELETE FROM CTE WHERE rn > 1;