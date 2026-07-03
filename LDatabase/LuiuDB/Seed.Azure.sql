PRINT N'開始匯入 Azure seed 資料';
GO

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

-- 需要的時候再手動取消註解
-- :r .\Seeds\dbo.tRole.Table.sql
-- :r .\Seeds\dbo.tSystemConfigs.Table.sql

GO

PRINT N'Azure seed 資料匯入完成';
GO
