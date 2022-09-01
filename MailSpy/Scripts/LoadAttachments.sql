USE {0}

SELECT
	AttachmentFile.File_Path AS Value
FROM fwk.t_ntf_AttachMailGenerated Attachment
INNER JOIN fwk.t_fwk_File AttachmentFile ON Attachment.File_ID = AttachmentFile.File_ID
WHERE Attachment.MailGenerated_ID = @id;