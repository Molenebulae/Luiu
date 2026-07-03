CREATE TABLE [dbo].[tReferences] (
    [ReferenceID]     INT IDENTITY (1000001, 1) NOT NULL,
    [TypeID]          INT NOT NULL,
    [ReferenceItemID] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([ReferenceID] ASC)
);

