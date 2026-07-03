CREATE TABLE [dbo].[tEventTagRelations] (
    [EventID] INT NOT NULL,
    [TagID]   INT NOT NULL,
    CONSTRAINT [PK_EventTagRelation] PRIMARY KEY CLUSTERED ([EventID] ASC, [TagID] ASC)
);

