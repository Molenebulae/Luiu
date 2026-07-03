CREATE TABLE [dbo].[tRouteCache] (
    [CacheID]     INT            IDENTITY (1001, 1) NOT NULL,
    [CacheKey]    NVARCHAR (50)  NOT NULL,
    [SpotIdsJson] NVARCHAR (500) NOT NULL,
    [TravelMode]  NVARCHAR (20)  NOT NULL,
    [ResultJson]  NVARCHAR (MAX) NOT NULL,
    [CreatedAt]   DATETIME       CONSTRAINT [DF_tRouteCache_CreatedAt] DEFAULT (getdate()) NOT NULL,
    [ExpiredAt]   DATETIME       NOT NULL,
    CONSTRAINT [PK_tRouteCache] PRIMARY KEY CLUSTERED ([CacheID] ASC)
);

