CREATE TABLE [dbo].[tRegions] (
    [RegionID]   INT           IDENTITY (1001, 1) NOT NULL,
    [RegionName] NVARCHAR (50) NOT NULL,
    CONSTRAINT [PK_SpotRegion] PRIMARY KEY CLUSTERED ([RegionID] ASC)
);

