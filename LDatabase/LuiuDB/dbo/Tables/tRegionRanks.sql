CREATE TABLE [dbo].[tRegionRanks] (
    [RegionRankID] INT           IDENTITY (10001, 1) NOT NULL,
    [RegionID]     INT           NOT NULL,
    [RegionName]   NVARCHAR (50) NOT NULL,
    [RankType]     NVARCHAR (50) NULL,
    [RankDate]     DATETIME      CONSTRAINT [DF_RegionRank_RankDate] DEFAULT (getdate()) NOT NULL,
    [Rank]         INT           NULL,
    CONSTRAINT [PK_RegionRanks] PRIMARY KEY CLUSTERED ([RegionRankID] ASC)
);

