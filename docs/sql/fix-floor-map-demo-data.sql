USE [PoolHubDbDev];
GO

-- Run with UTF-8 input so Vietnamese text is not mangled:
-- sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i docs\sql\fix-floor-map-demo-data.sql

UPDATE floors
SET name = CASE
    WHEN floor_id = 1 THEN N'Tầng 1'
    WHEN floor_id = 2 THEN N'Tầng 2 - VIP'
    ELSE name
END,
is_active = 1
WHERE floor_id IN (1, 2);

UPDATE zones
SET name = CASE
    WHEN zone_id = 1 THEN N'Khu A'
    WHEN zone_id = 2 THEN N'Khu B'
    WHEN zone_id = 3 THEN N'Khu VIP'
    ELSE name
END,
is_active = 1
WHERE zone_id IN (1, 2, 3);

UPDATE venue_tables
SET table_name = CASE table_code
    WHEN N'A01' THEN N'Bàn A01'
    WHEN N'A02' THEN N'Bàn A02'
    WHEN N'B01' THEN N'Bàn B01 Carom'
    WHEN N'V01' THEN N'Bàn VIP 01'
    WHEN N'V02' THEN N'Bàn VIP 02'
    ELSE table_name
END,
is_active = 1
WHERE table_code IN (N'A01', N'A02', N'B01', N'V01', N'V02');

SELECT COUNT(*) AS active_tables
FROM venue_tables
WHERE is_active = 1;
GO
