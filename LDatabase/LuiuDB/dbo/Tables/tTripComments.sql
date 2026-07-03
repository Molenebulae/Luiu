CREATE TABLE [dbo].[tTripComments] (
    [CommentID] INT           IDENTITY (10000001, 1) NOT NULL,
    [Content]   NVARCHAR (50) NOT NULL,
    [CreateAt]  DATETIME      DEFAULT (getdate()) NOT NULL,
    [ParentID]  INT           NULL,
    [TripID]    INT           NOT NULL,
    [UserID]    INT           NOT NULL,
    [IsDeleted] BIT           DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([CommentID] ASC)
);

