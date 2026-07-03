CREATE TABLE [dbo].[tPosts] (
    [PostID]        INT            IDENTITY (1000001, 1) NOT NULL,
    [CategoryID]    INT            NOT NULL,
    [PostTitle]     NVARCHAR (50)  NOT NULL,
    [MemberID]      INT            NOT NULL,
    [PostContent]   NVARCHAR (500) NOT NULL,
    [PostTime]      DATETIME       CONSTRAINT [DF__Posts__PostTime__4AB81AF0] DEFAULT (getdate()) NOT NULL,
    [IsDelete]      BIT            CONSTRAINT [DF_Posts_IsPostDelete] DEFAULT ((0)) NOT NULL,
    [CommentAmount] INT            CONSTRAINT [DF__Posts__CommentAm__4BAC3F29] DEFAULT ((0)) NOT NULL,
    [ViewCount]     INT            CONSTRAINT [DF__Posts__ViewCount__4CA06362] DEFAULT ((0)) NOT NULL,
    [TagAmount]     INT            NULL,
    [ReferenceID]   INT            NULL,
    [IsEdit]        BIT            CONSTRAINT [DF_Posts_IsEdit] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK__Posts__AA12603828164C07] PRIMARY KEY CLUSTERED ([PostID] ASC)
);

