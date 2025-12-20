-- Script ?? thêm c?t BLACK_LIST vào b?ng CUSTOMER và insert d? li?u m?u

-- 1. Ki?m tra và thêm c?t BLACK_LIST n?u ch?a t?n t?i
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CUSTOMER]') AND name = 'BLACK_LIST')
BEGIN
    ALTER TABLE [dbo].[CUSTOMER]
    ADD BLACK_LIST BIT NOT NULL DEFAULT 0;
    
    PRINT '?ã thêm c?t BLACK_LIST vào b?ng CUSTOMER';
END
ELSE
BEGIN
    PRINT 'C?t BLACK_LIST ?ã t?n t?i trong b?ng CUSTOMER';
END
GO

-- 2. Insert 2 customer m?u (n?u ch?a t?n t?i)
-- Customer 1: BlackList = false (0)
IF NOT EXISTS (SELECT * FROM [dbo].[CUSTOMER] WHERE EMAIL = 'nguyenvana@example.com')
BEGIN
    INSERT INTO [dbo].[CUSTOMER] (CUSTOMER_ID, FIRST_NAME, LAST_NAME, EMAIL, PHONE, ID_NO, DOB, CREATED_DATE, BLACK_LIST)
    VALUES (
        NULL,
        N'Nguy?n V?n',
        N'A',
        'nguyenvana@example.com',
        '0901234567',
        'HE170001',
        '2000-01-15',
        GETDATE(),
        0  -- BlackList = false
    );
    
    PRINT '?ã insert customer: Nguy?n V?n A (BlackList = false)';
END
GO

-- Customer 2: BlackList = true (1)
IF NOT EXISTS (SELECT * FROM [dbo].[CUSTOMER] WHERE EMAIL = 'tranthib@example.com')
BEGIN
    INSERT INTO [dbo].[CUSTOMER] (CUSTOMER_ID, FIRST_NAME, LAST_NAME, EMAIL, PHONE, ID_NO, DOB, CREATED_DATE, BLACK_LIST)
    VALUES (
        NULL,
        N'Tr?n Th?',
        N'B',
        'tranthib@example.com',
        '0909876543',
        'HE170002',
        '1999-05-20',
        GETDATE(),
        1  -- BlackList = true
    );
    
    PRINT '?ã insert customer: Tr?n Th? B (BlackList = true)';
END
GO

-- 3. Ki?m tra k?t qu?
SELECT 
    ID,
    FIRST_NAME,
    LAST_NAME,
    EMAIL,
    PHONE,
    BLACK_LIST,
    CREATED_DATE
FROM [dbo].[CUSTOMER]
WHERE EMAIL IN ('nguyenvana@example.com', 'tranthib@example.com')
ORDER BY BLACK_LIST, FIRST_NAME;

PRINT 'Hoàn thành! Vui lòng ch?y l?i scaffold command ?? c?p nh?t models.';
