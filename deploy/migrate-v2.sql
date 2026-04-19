BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419083222_AddMedicalDeclarationFields'
)
BEGIN
    ALTER TABLE [kiosk].[SiteRegisterEntries] ADD [AdditionalInfo] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419083222_AddMedicalDeclarationFields'
)
BEGIN
    ALTER TABLE [kiosk].[SiteRegisterEntries] ADD [MedicalStatus] nvarchar(32) NOT NULL DEFAULT N'NotDeclared';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419083222_AddMedicalDeclarationFields'
)
BEGIN
    ALTER TABLE [kiosk].[SiteRegisterEntries] ADD [SiteCode] nvarchar(32) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419083222_AddMedicalDeclarationFields'
)
BEGIN
    ALTER TABLE [kiosk].[SiteRegisterEntries] ADD [SiteCodeGenerated] nvarchar(32) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419083222_AddMedicalDeclarationFields'
)
BEGIN
    ALTER TABLE [kiosk].[SiteRegisterEntries] ADD [Status] nvarchar(32) NOT NULL DEFAULT N'OnSite';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419083222_AddMedicalDeclarationFields'
)
BEGIN
    UPDATE kiosk.SiteRegisterEntries SET MedicalStatus = 'Fit' WHERE MedicalStatus = 'NotDeclared' AND Status = 'OnSite';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419083222_AddMedicalDeclarationFields'
)
BEGIN
    CREATE INDEX [IX_SiteRegisterEntries_DateIn_Status] ON [kiosk].[SiteRegisterEntries] ([DateIn], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419083222_AddMedicalDeclarationFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260419083222_AddMedicalDeclarationFields', N'8.0.26');
END;
GO

COMMIT;
GO

