CREATE TABLE [dbo].[tTypes] (
    [TypeID]   INT           IDENTITY (10000001, 1) NOT NULL,
    [TypeName] NVARCHAR (20) NOT NULL,
    CONSTRAINT [PK_Types] PRIMARY KEY CLUSTERED ([TypeID] ASC)
);

