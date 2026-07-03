CREATE TABLE [dbo].[tEventSpotRelations] (
    [EventID]  INT NOT NULL,
    [SpotID]   INT NOT NULL,
    [RegionID] INT NOT NULL,
    CONSTRAINT [PK_EventSpotRelation] PRIMARY KEY CLUSTERED ([EventID] ASC, [SpotID] ASC)
);

