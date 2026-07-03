CREATE TABLE [dbo].[tVerifications] (
    [VerificationID] INT           IDENTITY (10000001, 1) NOT NULL,
    [Type]           TINYINT       NOT NULL,
    [TokenHash]      VARCHAR (128) NOT NULL,
    [CreatedAt]      DATETIME      CONSTRAINT [DF_Verification_CreatedAt] DEFAULT (getdate()) NOT NULL,
    [RequestIP]      VARCHAR (50)  NOT NULL,
    [Attempts]       INT           CONSTRAINT [DF_Verification_Attempts] DEFAULT ((0)) NOT NULL,
    [MemberID]       INT           NOT NULL,
    [Status]         TINYINT       CONSTRAINT [DF_tVerifications_Status] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Verification] PRIMARY KEY CLUSTERED ([VerificationID] ASC)
);

