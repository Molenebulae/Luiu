CREATE TABLE [dbo].[tMemories] (
    [MemoryID]      INT            IDENTITY (1000001, 1) NOT NULL,
    [UserID]        INT            NOT NULL,
    [Title]         NVARCHAR (100) NOT NULL,
    [CoverImage]    NVARCHAR (255) NULL,
    [StartDate]     DATE           NULL,
    [EndDate]       DATE           NULL,
    [ReviewStatus]  INT            DEFAULT ((0)) NOT NULL,
    [ViewCount]     INT            DEFAULT ((0)) NOT NULL,
    [LikeCount]     INT            DEFAULT ((0)) NOT NULL,
    [FavoriteCount] INT            DEFAULT ((0)) NOT NULL,
    [OfficeOper]    SMALLINT       DEFAULT ((0)) NULL,
    [SourceTripId]  INT            NULL,
    [IsDelete]      BIT            DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([MemoryID] ASC)
);

