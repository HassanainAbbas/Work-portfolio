CREATE TABLE [dbo].[Products] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Price] DECIMAL(18, 2) NOT NULL,
    [ImageUrl] NVARCHAR(MAX) NULL,
    [Category] NVARCHAR(100) NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);
