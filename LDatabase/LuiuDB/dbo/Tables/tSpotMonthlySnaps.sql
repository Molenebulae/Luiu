CREATE TABLE [dbo].[tSpotMonthlySnaps] (
    [SnapID]        BIGINT   IDENTITY (10001, 1) NOT NULL,
    [SpotID]        INT      NOT NULL,
    [RegionID]      INT      NOT NULL,
    [SnapYear]      INT      NOT NULL,
    [SnapMonth]     INT      NOT NULL,
    [ViewCount]     INT      CONSTRAINT [DF_SpotMonthlySnap_ViewCount] DEFAULT ((0)) NOT NULL,
    [FavoriteCount] INT      CONSTRAINT [DF_SpotMonthlySnap_FavoriteCount] DEFAULT ((0)) NOT NULL,
    [PlanCount]     INT      CONSTRAINT [DF_SpotMonthlySnap_PlanCount] DEFAULT ((0)) NOT NULL,
    [RecordCount]   INT      CONSTRAINT [DF_SpotMonthlySnap_RecordCount] DEFAULT ((0)) NOT NULL,
    [RefCount]      INT      CONSTRAINT [DF_SpotMonthlySnap_RefCount] DEFAULT ((0)) NOT NULL,
    [SnapTime]      DATETIME CONSTRAINT [DF_SpotMonthlySnap_SnapTime] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_SpotMonthlySnap] PRIMARY KEY CLUSTERED ([SnapID] ASC)
);

