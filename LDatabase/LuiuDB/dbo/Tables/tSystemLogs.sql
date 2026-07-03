CREATE TABLE [dbo].[tSystemLogs] (
    [Id]              INT            IDENTITY (1, 1) NOT NULL,
    [Message]         NVARCHAR (MAX) NULL,
    [MessageTemplate] NVARCHAR (MAX) NULL,
    [Level]           NVARCHAR (16)  NULL,
    [TimeStamp]       DATETIME       NULL,
    [Exception]       NVARCHAR (MAX) NULL,
    [Properties]      NVARCHAR (MAX) NULL,
    [Source]          NVARCHAR (50)  NULL,
    [Developer]       NVARCHAR (50)  NULL,
    CONSTRAINT [PK_tSystemLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);

