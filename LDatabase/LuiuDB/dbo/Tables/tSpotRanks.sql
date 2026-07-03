CREATE TABLE [dbo].[tSpotRanks] (
    [SpotRankID] INT           IDENTITY (100001, 1) NOT NULL,
    [SpotID]     INT           NOT NULL,
    [SpotName]   NVARCHAR (50) NOT NULL,
    [RankType]   NVARCHAR (50) NULL,
    [RankDate]   DATETIME      CONSTRAINT [DF_SpotRank_RankDate] DEFAULT (getdate()) NOT NULL,
    [Rank]       INT           NULL,
    CONSTRAINT [PK_SpotRank] PRIMARY KEY CLUSTERED ([SpotRankID] ASC)
);

