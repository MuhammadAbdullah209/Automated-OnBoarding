-- Read-only checks for every table in the AutomatedOnboarding database.
-- Select one query and press F5 (SSMS) or Ctrl+Shift+E (Visual Studio) to run just that one.
USE AutomatedOnboarding;
GO

/* ============ ACCOUNTS & LOGIN ============ */

-- 1. Logins for every role (password hash left out on purpose)
SELECT Id, Email, UserType, EmailConfirmed, IsActive, CreatedAt
FROM UserAuths
ORDER BY CreatedAt DESC;

-- 2. Internal users (staff) with their login email
SELECT i.Id, i.FullName, u.Email, u.UserType, u.IsActive
FROM InternalUsers i
JOIN UserAuths u ON u.Id = i.UserAuthId;

-- 3. Partners with their login email (PCIPAL is the built-in one)
SELECT p.Id, p.Name, u.Email, p.IsActive, p.CreatedAt
FROM Partners p
JOIN UserAuths u ON u.Id = p.UserAuthId
ORDER BY p.CreatedAt DESC;

-- 4. Customers, their partner, and login email (NULL login = partner-created customer)
SELECT c.Id, c.CompanyName, c.IsDirect, c.Status, p.Name AS PartnerName, u.Email, c.CreatedAt
FROM Customers c
JOIN Partners p ON p.Id = c.PartnerId
LEFT JOIN UserAuths u ON u.Id = c.UserAuthId
ORDER BY c.CreatedAt DESC;

-- 5. Email verification tokens / password reset OTPs (hash only, never the code itself)
SELECT t.Id, u.Email, t.Purpose, t.CreatedAt, t.ExpiresAt, t.ConsumedAt, t.FailedAttempts
FROM OtpTokens t
JOIN UserAuths u ON u.Id = t.UserAuthId
ORDER BY t.CreatedAt DESC;


/* ============ CUSTOMER DOCUMENTS ============ */

-- 6. Document types customers are asked for (managed by internal users)
SELECT * FROM DocumentTypes ORDER BY CreatedAt DESC;

-- 7. Documents customers uploaded
SELECT d.Id, c.CompanyName, t.Name AS DocumentType, d.OriginalFileName, d.FileSizeBytes, d.UploadedAt
FROM CustomerDocuments d
JOIN Customers c ON c.Id = d.CustomerId
JOIN DocumentTypes t ON t.Id = d.DocumentTypeId
ORDER BY d.UploadedAt DESC;


/* ============ PROJECTS ============ */

-- 8. Projects with status, build type and reasons
SELECT p.Id, p.Name, c.CompanyName, p.Status, p.CurrentStep, p.BuildType, p.BuildReasons,
       p.UsersCsvRequestedAt, p.DeletionScheduledFor, p.CreatedAt
FROM Projects p
JOIN Customers c ON c.Id = p.CustomerId
ORDER BY p.CreatedAt DESC;

-- 9. Step 1: Discovery
SELECT * FROM ProjectDiscoveries ORDER BY CreatedAt DESC;

-- 10. Step 2: Technical environment
SELECT * FROM ProjectTechnicalEnvironments ORDER BY CreatedAt DESC;

-- 11. Step 3: Payment configuration (with the chosen gateway's name)
SELECT pc.*, g.Name AS GatewayName
FROM ProjectPaymentConfigurations pc
LEFT JOIN PaymentGateways g ON g.Id = pc.PaymentGatewayId
ORDER BY pc.CreatedAt DESC;

-- 12. Step 4: Adaptive configuration (gateway configuration)
SELECT * FROM ProjectGatewayConfigurations ORDER BY CreatedAt DESC;

-- 13. Step 4: Gateway fields chosen per project
SELECT f.ProjectId, p.Name AS ProjectName, f.Name AS FieldName, f.FieldType, f.IsInput
FROM ProjectGatewayFields f
JOIN Projects p ON p.Id = f.ProjectId
ORDER BY p.Name, f.FieldType, f.Name;

-- 14. Project documents (API docs, information request files)
SELECT d.Id, p.Name AS ProjectName, d.Type, d.OriginalFileName, d.FileSizeBytes, d.UploadedAt
FROM ProjectDocuments d
JOIN Projects p ON p.Id = d.ProjectId
ORDER BY d.UploadedAt DESC;

-- 15. Provisioning details (client secret left out on purpose)
SELECT pd.ProjectId, p.Name AS ProjectName, pd.Platform, pd.TenantName, pd.TenantId, pd.Region,
       pd.FlowId, pd.AccountCreatedAt, pd.CreatedAt
FROM ProjectProvisioningDetails pd
JOIN Projects p ON p.Id = pd.ProjectId
ORDER BY pd.CreatedAt DESC;

-- 16. Users from the uploaded users CSV (the customer's call-centre staff)
SELECT cu.ProjectId, p.Name AS ProjectName, cu.Name, cu.Email, cu.Role, cu.CreatedAt
FROM CustomerUsers cu
JOIN Projects p ON p.Id = cu.ProjectId
ORDER BY cu.CreatedAt DESC;


/* ============ INFORMATION REQUESTS (custom projects) ============ */

-- 17. Requests staff sent
SELECT r.Id, p.Name AS ProjectName, r.Status, r.RequestedByName, r.CreatedAt, r.CustomerRespondedAt
FROM ProjectInformationRequests r
JOIN Projects p ON p.Id = r.ProjectId
ORDER BY r.CreatedAt DESC;

-- 18. Items inside each request
SELECT i.Id, i.RequestId, i.Code, i.Title, i.Status, i.SubmittedAt, i.ReviewedAt, i.ReviewNote
FROM ProjectInformationRequestItems i
ORDER BY i.SubmittedAt DESC;

-- 19. Customer responses to items, and staff review outcome
SELECT r.Id, r.ItemId, r.Note, r.Outcome, r.ReviewNote, r.ReviewedByName, r.SubmittedAt, r.ReviewedAt
FROM ProjectInformationResponses r
ORDER BY r.SubmittedAt DESC;


/* ============ GATEWAY SETUP DATA ============ */

-- 20. Payment gateways offered on step 3
SELECT * FROM PaymentGateways ORDER BY Name;

-- 21. Fields each gateway offers on step 4
SELECT g.Name AS GatewayName, f.*
FROM PaymentGatewayFields f
JOIN PaymentGateways g ON g.Id = f.PaymentGatewayId
ORDER BY g.Name;


/* ============ LOGS & SECURITY ============ */

-- 22. Activity history (customer and project events)
SELECT TOP 100 a.CreatedAt, c.CompanyName, a.ProjectId, a.Target, a.Action, a.Reason, a.ActorName, a.ActorRole
FROM ActivityLogs a
LEFT JOIN Customers c ON c.Id = a.CustomerId
ORDER BY a.CreatedAt DESC;

-- 23. Visitor IPs and the block list
SELECT IpAddress, Country, City, HitCount, LastPath, LastUserName, IsBlocked, BlockedReason, FirstSeenAt, LastSeenAt
FROM VisitorIps
ORDER BY LastSeenAt DESC;

-- 24. Applied migrations
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId DESC;


/* ============ QUICK ROW COUNTS ============ */

-- 25. How many rows every table has
SELECT t.name AS TableName, SUM(p.rows) AS RowCount
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
GROUP BY t.name
ORDER BY t.name;
