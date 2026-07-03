PRINT '--- 開始執行部署後腳本 ---';

-- 根據各表的 identity的初始值，刪除舊的種子資料 
PRINT '1. 清除種子資料..........';

-- 用於接收資料
DECLARE @CleanTableName NVARCHAR(30);
DECLARE @CleanColumnName NVARCHAR(30);
DECLARE @CleanSeed INT;
DECLARE @CleanSQL NVARCHAR(MAX);
-- 儲存每個資料表的預設值
DECLARE @IdentityList TABLE (
    TableName NVARCHAR(30),
    ColumnName NVARCHAR(30),
    SeedValue INT
);

-- 定義游標
DECLARE CleanCursor CURSOR FOR
SELECT
    QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) AS TableName,
    CAST(ic.seed_value AS int) AS SeedValue
FROM sys.tables AS t
    JOIN sys.identity_columns AS ic ON t.object_id = ic.object_id;

-- 開啟游標
OPEN CleanCursor;
-- 讀取第一筆
FETCH NEXT FROM CleanCursor INTO @CleanTableName, @CleanSeed;

PRINT '1.1. 清除有預設值的資料表'
WHILE @@FETCH_STATUS = 0
BEGIN
    -- 儲存預設值爛位名稱
    Select @CleanColumnName = QUOTENAME(name)
    From sys.identity_columns
    WHERE object_id = OBJECT_ID(@CleanTableName);

    SET @CleanSQL = N'Delete From ' + @CleanTableName +
                    N' Where ' + @CleanColumnName +
                    N' < ' + CAST(@CleanSeed AS nvarchar);

    INSERT INTO @IdentityList (TableName, ColumnName, SeedValue)
    VALUES (@CleanTableName, @CleanColumnName, @CleanSeed);

    PRINT @CleanSQL;
    EXEC sp_executesql @CleanSQL;
    FETCH NEXT FROM CleanCursor INTO @CleanTableName, @CleanSeed;
END

-- 關閉資源
CLOSE CleanCursor;
DEALLOCATE CleanCursor;


PRINT '1.2. 清除沒有預設值得資料表'

DECLARE @SpotSeed INT;
DECLARE @TypeSeed INT;
DECLARE @TagSeed INT;
DECLARE @EventSeed INT;
DECLARE @RegionSeed INT;
DECLARE @MemberSeed INT;

SELECT @SpotSeed = SeedValue FROM @IdentityList WHERE TableName = '[dbo].[tSpots]';
SELECT @TypeSeed = SeedValue FROM @IdentityList WHERE TableName = '[dbo].[tSpotTypes]';
SELECT @TagSeed = SeedValue FROM @IdentityList WHERE TableName = '[dbo].[tTags]';
SELECT @EventSeed = SeedValue FROM @IdentityList WHERE TableName = '[dbo].[tEvents]';
SELECT @RegionSeed = SeedValue FROM @IdentityList WHERE TableName = '[dbo].[tRegions]';
SELECT @MemberSeed = SeedValue FROM @IdentityList WHERE TableName = '[dbo].[tMembers]';

PRINT 'DELETE FROM tSpotTypeRelations WHERE SpotID < '+ CAST(@SpotSeed AS VARCHAR) +' OR TypeID < '+ CAST(@TypeSeed AS VARCHAR);
DELETE FROM tSpotTypeRelations
WHERE 
    SpotID < @SpotSeed
    OR
    TypeID < @TypeSeed;

PRINT 'DELETE FROM tSpotTagRelations WHERE SpotID < '+ CAST(@SpotSeed AS VARCHAR) +' OR SpotTagID < '+ CAST(@TagSeed AS VARCHAR);
DELETE FROM tSpotTagRelations
WHERE
    SpotID < @SpotSeed
    OR
    SpotTagID < @TagSeed

PRINT 'DELETE FROM tEventSpotRelations WHERE EventID < '+ CAST(@EventSeed AS VARCHAR) +' OR SpotID < '+ CAST(@SpotSeed AS VARCHAR) +' OR RegionID < '+ CAST(@RegionSeed AS VARCHAR);
DELETE FROM tEventSpotRelations
WHERE 
    EventID < @EventSeed
    OR
    SpotID < @SpotSeed
    OR
    RegionID < @RegionSeed

PRINT 'DELETE FROM tEventTagRelations WHERE EventID < '+ CAST(@EventSeed AS VARCHAR) +' OR TagID < '+ CAST(@TagSeed AS VARCHAR);
DELETE FROM tEventTagRelations
WHERE 
    EventID < @EventSeed
    OR
    TagID < @TagSeed

PRINT 'DELETE FROM tFollows WHERE FollowerID < '+ CAST(@MemberSeed AS VARCHAR) +' OR FollowingID < '+ CAST(@MemberSeed AS VARCHAR);
DELETE FROM tFollows
WHERE
    FollowerID < @MemberSeed
    OR
    FollowingID < @MemberSeed

PRINT 'DELETE FROM tOAuthLogins WHERE MemberID < '+ CAST(@MemberSeed AS VARCHAR);
DELETE FROM tOAuthLogins
WHERE MemberID < @MemberSeed

-- 不更新tRole跟tSystemConfigs
-- DELETE FROM tRole
-- DELETE FROM tSystemConfigs
GO

PRINT '2. 匯入資料..........';

:r .\Seeds\dbo.tAdminLogs.Table.sql
:r .\Seeds\dbo.tCategories.Table.sql
:r .\Seeds\dbo.tCollects.Table.sql
:r .\Seeds\dbo.tCommentLikes.Table.sql
:r .\Seeds\dbo.tComments.Table.sql
:r .\Seeds\dbo.tEventRanks.Table.sql
:r .\Seeds\dbo.tEvents.Table.sql
:r .\Seeds\dbo.tEventSpotRelations.Table.sql
:r .\Seeds\dbo.tEventTagRelations.Table.sql
:r .\Seeds\dbo.tExpenses.Table.sql
:r .\Seeds\dbo.tExpensesSplits.Table.sql
:r .\Seeds\dbo.tFollows.Table.sql
:r .\Seeds\dbo.tLoginLogs.Table.sql
:r .\Seeds\dbo.tMembers.Table.sql
:r .\Seeds\dbo.tMemories.Table.sql
:r .\Seeds\dbo.tMemoryDays.Table.sql
:r .\Seeds\dbo.tMemoryStops.Table.sql
:r .\Seeds\dbo.tOAuthLogins.Table.sql
:r .\Seeds\dbo.tPackingCategories.Table.sql
:r .\Seeds\dbo.tPackingItems.Table.sql
:r .\Seeds\dbo.tPostLikes.Table.sql
:r .\Seeds\dbo.tPosts.Table.sql
:r .\Seeds\dbo.tPostTags.Table.sql
:r .\Seeds\dbo.tReferences.Table.sql
:r .\Seeds\dbo.tRegionRanks.Table.sql
:r .\Seeds\dbo.tRegions.Table.sql
:r .\Seeds\dbo.tReports.Table.sql
:r .\Seeds\dbo.tRouteCache.Table.sql
:r .\Seeds\dbo.tSpotMonthlySnaps.Table.sql
:r .\Seeds\dbo.tSpotRanks.Table.sql
:r .\Seeds\dbo.tSpots.Table.sql
:r .\Seeds\dbo.tSpotTagRelations.Table.sql
:r .\Seeds\dbo.tSpotTypeRelations.Table.sql
:r .\Seeds\dbo.tSpotTypes.Table.sql
:r .\Seeds\dbo.tTags.Table.sql
:r .\Seeds\dbo.tTripCollaborators.Table.sql
:r .\Seeds\dbo.tTripComments.Table.sql
:r .\Seeds\dbo.tTripDetails.Table.sql
:r .\Seeds\dbo.tTrips.Table.sql
:r .\Seeds\dbo.tTripSuggests.Table.sql
:r .\Seeds\dbo.tTypes.Table.sql
:r .\Seeds\dbo.tUserPackingLists.Table.sql
:r .\Seeds\dbo.tVerifications.Table.sql
:r .\Seeds\dbo.tViolationRules.Table.sql
-- 需要的時候取消註解
-- :r .\Seeds\dbo.tRole.Table.sql
-- :r .\Seeds\dbo.tSystemConfigs.Table.sql

GO

-- PRINT '3. 自動矯正所有資料表的計數器';
-- -- 用於接收資料
-- DECLARE @ReTableName NVARCHAR(30);
-- DECLARE @ReSeed INT;
-- DECLARE @ReSQL NVARCHAR(MAX);
-- DECLARE @MaxIDInTable bigint;

-- -- 定義游標
-- DECLARE ReCursor CURSOR FOR
-- SELECT
--     QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) AS TableName,
--     CAST(ic.seed_value AS int) AS SeedValue
-- FROM sys.tables AS t
--     JOIN sys.identity_columns AS ic ON t.object_id = ic.object_id;

-- -- 開啟游標
-- OPEN ReCursor;
-- -- 讀取第一筆
-- FETCH NEXT FROM ReCursor INTO @ReTableName, @ReSeed;

-- WHILE @@FETCH_STATUS = 0
-- BEGIN
--     -- 取得id最大值
--     -- 1. 找到ID 欄位名稱
--     DECLARE @IDColName NVARCHAR(128);
--     SELECT @IDColName = QUOTENAME(name)
--     FROM sys.identity_columns
--     WHERE object_id = OBJECT_ID(@ReTableName);

--     -- 2. 取得id最大值
--     DECLARE @ParamDef NVARCHAR(500) = N'@MaxOut bigint OUTPUT';
--     DECLARE @GetMaxSQL NVARCHAR(MAX) = N'Select @MaxOut = Max(' + @IDColName + ') From ' + @ReTableName;

--     EXEC sp_executesql @GetMaxSQL, @ParamDef, @MaxOut = @MaxIDInTable OUTPUT;

--     -- 3. 邏輯判斷
--     DECLARE @FinalReseedValue bigint;

--     -- 如果沒有資料預設為0
--     SET @MaxIDInTable = ISNULL(@MaxIDInTable, 0);

--     IF (@MaxIDInTable >= @ReSeed)
--         SET @FinalReseedValue = @MaxIDInTable;
--     ELSE
--         SET @FinalReseedValue = @ReSeed;

--     -- 4. 執行重設
--     SET @ReSQL = N'DBCC CHECKIDENT(''' + @ReTableName + ''', RESEED, ' + CAST(@FinalReseedValue AS NVARCHAR) + ')';
--     EXEC sp_executesql @ReSQL;

--     PRINT ' > ' + @ReTableName + ' 計數器已重設至 ' + CAST(@FinalReseedValue as NVARCHAR);

--     -- EXEC sp_executesql @ReSQL;
--     FETCH NEXT FROM ReCursor INTO @ReTableName, @ReSeed;
-- END

-- -- 關閉資源
-- CLOSE ReCursor;
-- DEALLOCATE ReCursor;
-- GO

PRINT N'部署完成';
GO