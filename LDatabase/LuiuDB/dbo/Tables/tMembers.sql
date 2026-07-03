CREATE TABLE [dbo].[tMembers] (
    [MemberID]      INT            IDENTITY (100001, 1) NOT NULL,
    [UserID]        VARCHAR (30)   NOT NULL,
    [Email]         VARCHAR (100)  NOT NULL,
    [Password]      VARCHAR (255)  NOT NULL,
    [Name]          NVARCHAR (50)  NOT NULL,
    [Phone]         VARCHAR (10)   NULL,
    [Birthday]      DATE           NULL,
    [Gender]        TINYINT        CONSTRAINT [DF_Members_Gender] DEFAULT ((0)) NOT NULL,
    [AvatarUrl]     NVARCHAR (255) NULL,
    [CreateDate]    DATETIME       CONSTRAINT [DF_Members_CreateDate] DEFAULT (getdate()) NOT NULL,
    [UpdateDate]    DATETIME       CONSTRAINT [DF_Members_UpdateDate] DEFAULT (getdate()) NOT NULL,
    [ProfileStatus] BIT            CONSTRAINT [DF_Members_ProfileStatus] DEFAULT ((0)) NOT NULL,
    [Status]        TINYINT        CONSTRAINT [DF_Members_Status] DEFAULT ((0)) NOT NULL,
    [LockReason]    NVARCHAR (50)  NULL,
    [IsDelete]      BIT            CONSTRAINT [DF_Members_IsDelete] DEFAULT ((0)) NOT NULL,
    [DeleteTime]    DATETIME       NULL,
    [RoleID]        INT            NOT NULL,
    [Bio]           NVARCHAR (200) NULL,
    CONSTRAINT [PK_Members] PRIMARY KEY CLUSTERED ([MemberID] ASC)
);

