CREATE TABLE [dbo].[tTripSuggests] (
    [SuggesterID]     INT            IDENTITY (10000001, 1) NOT NULL,
    [VersionName]     NVARCHAR (10)  NOT NULL,
    [Summary]         NVARCHAR (100) NULL,
    [CreateAt]        DATETIME       DEFAULT (getdate()) NOT NULL,
    [UpdateAt]        DATETIME       DEFAULT (getdate()) NULL,
    [IsApproved]      BIT            DEFAULT ((0)) NULL,
    [SuggestByUserID] INT            NOT NULL,
    [TripID]          INT            NOT NULL,
    [IsDeleted]       BIT            DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([SuggesterID] ASC)
);

