CREATE TABLE [dbo].[tOAuthLogins] (
    [AuthType]            VARCHAR (10)   NOT NULL,
    [ProviderKey]         NVARCHAR (255) NOT NULL,
    [CreateTime]          DATETIME       CONSTRAINT [DF_OAuthLogin_CreateTime] DEFAULT (getdate()) NOT NULL,
    [ProviderDisplayName] VARCHAR (50)   NULL,
    [MemberID]            INT            NOT NULL,
    CONSTRAINT [PK_OAuthLogin] PRIMARY KEY CLUSTERED ([AuthType] ASC, [ProviderKey] ASC)
);

