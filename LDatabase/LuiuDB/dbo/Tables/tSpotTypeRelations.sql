CREATE TABLE [dbo].[tSpotTypeRelations] (
    [SpotID] INT NOT NULL,
    [TypeID] INT NOT NULL,
    CONSTRAINT [PK_SpotType] PRIMARY KEY CLUSTERED ([SpotID] ASC, [TypeID] ASC)
);

