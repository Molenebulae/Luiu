CREATE TABLE [dbo].[tEventRanks] (
    [EventRankID] INT           IDENTITY (10001, 1) NOT NULL,
    [EventID]     INT           NOT NULL,
    [EventName]   NVARCHAR (50) NOT NULL,
    [RankDate]    DATETIME      CONSTRAINT [DF_EventRank_RankDate] DEFAULT (getdate()) NOT NULL,
    [Rank]        INT           NULL,
    CONSTRAINT [PK_EventRank] PRIMARY KEY CLUSTERED ([EventRankID] ASC)
);

