CREATE TABLE [dbo].[tSpotTypes] (
    [TypeID]   INT           IDENTITY (101, 1) NOT NULL,
    [TypeName] NVARCHAR (50) NOT NULL,
    CONSTRAINT [PK_SpotType_1] PRIMARY KEY CLUSTERED ([TypeID] ASC)
);

