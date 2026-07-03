CREATE TABLE [dbo].[tMemoryDays] (
    [DayID]     INT  IDENTITY (10000001, 1) NOT NULL,
    [MemoryID]  INT  NOT NULL,
    [DayNumber] INT  NOT NULL,
    [DayDate]   DATE NULL,
    PRIMARY KEY CLUSTERED ([DayID] ASC)
);

