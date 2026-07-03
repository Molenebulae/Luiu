CREATE TABLE [dbo].[tSpotTagRelations] (
    [SpotID]    INT NOT NULL,
    [SpotTagID] INT NOT NULL,
    CONSTRAINT [PK_SpotTagRelation] PRIMARY KEY CLUSTERED ([SpotID] ASC, [SpotTagID] ASC)
);

