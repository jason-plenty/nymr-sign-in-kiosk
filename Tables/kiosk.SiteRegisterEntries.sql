CREATE TABLE [kiosk].[SiteRegisterEntries]
(
[Id] [uniqueidentifier] NOT NULL,
[Name] [nvarchar] (200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[Organisation] [nvarchar] (200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[SignatureUrl] [nvarchar] (2048) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
[DateIn] [date] NOT NULL,
[TimeIn] [time] NOT NULL,
[TimeOut] [time] NULL,
[CreatedAtUtc] [datetimeoffset] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [kiosk].[SiteRegisterEntries] ADD CONSTRAINT [PK_SiteRegisterEntries] PRIMARY KEY CLUSTERED ([Id]) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_SiteRegisterEntries_DateIn] ON [kiosk].[SiteRegisterEntries] ([DateIn]) ON [PRIMARY]
GO
