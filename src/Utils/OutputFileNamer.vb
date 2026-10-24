Imports System.Security.Cryptography
Imports System.Text

Namespace Utils

    ''' <summary>
    ''' Builds the final output zip file name.
    '''
    ''' Real-world testing showed that DIM requires the zip file name to
    ''' start with an "IM{id}-{variant}_" prefix to even recognize it as
    ''' installable, regardless of what Manifest.dsx/Supplement.dsx declare
    ''' inside. If the source zip doesn't already follow that convention,
    ''' a synthetic ID is generated here so the output is always accepted.
    '''
    ''' Casing rules for the readable part of the name:
    ''' - If the source zip already follows the "IM{id}-{variant}_{name}"
    '''   convention, its own short name is reused as-is (it's already a
    '''   vendor/DIM-approved value).
    ''' - If the user explicitly supplied a product name, that exact
    '''   capitalization is kept — only spaces are stripped.
    ''' - Otherwise (auto-suggested from an unrecognized file name), each
    '''   word is capitalized (Title Case) and spaces are stripped, e.g.
    '''   "rks maxine for genesis 9" -> "RksMaxineForGenesis9".
    ''' </summary>
    Public Module OutputFileNamer

        ''' <summary>
        ''' Synthetic IDs are generated in this range to stay clear of
        ''' typical (much lower) real Daz3D catalog product IDs.
        ''' </summary>
        Private Const SyntheticIdRangeStart As UInteger = 90000000
        Private Const SyntheticIdRangeSize As UInteger = 9999999 ' keeps result <= 99999999

        ''' <summary>
        ''' Suffix appended to the readable name. Kept with no separator
        ''' character in front of it (no "_" or "-"), since testing showed
        ''' DIM can misread the file name when one is present there.
        ''' </summary>
        Private Const OutputSuffix As String = "DIM"

        Public Function BuildOutputFileName(sourceZipFileName As String, parsed As ParsedZipName,
                                             Optional userProvidedProductName As String = Nothing) As String
            Dim productId As String
            Dim variantNumber As String

            If parsed.IsRecognized Then
                productId = parsed.ProductId
                variantNumber = parsed.VariantNumber
            Else
                Dim rawBase = ZipNameParser.StripZipExtension(sourceZipFileName)
                productId = GenerateDeterministicId(rawBase)
                variantNumber = "01"
            End If

            Dim nameForFileName As String

            If Not String.IsNullOrWhiteSpace(userProvidedProductName) Then
                ' The user chose this name explicitly: keep their exact
                ' capitalization, only remove spaces / invalid characters.
                nameForFileName = RemoveSpaces(userProvidedProductName)
            ElseIf parsed.IsRecognized Then
                ' Source zip already had a proper DIM-style short name.
                nameForFileName = RemoveSpaces(parsed.ShortName)
            Else
                ' Auto-suggested from an unrecognized file name: apply Title Case.
                nameForFileName = TitleCaseNoSpaces(parsed.SuggestedProductName)
            End If

            nameForFileName = StripInvalidFileNameChars(nameForFileName)

            Return $"DP{productId}-{variantNumber}_{nameForFileName}{OutputSuffix}.zip"
        End Function

        ''' <summary>
        ''' Derives an 8-digit numeric ID from the SHA-256 hash of the given
        ''' text. Deterministic: the same input always produces the same ID,
        ''' so reprocessing the same source zip yields a stable file name.
        ''' </summary>
        Private Function GenerateDeterministicId(seedText As String) As String
            Using sha As SHA256 = SHA256.Create()
                Dim hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(seedText.ToLowerInvariant().Trim()))
                Dim value = BitConverter.ToUInt32(hashBytes, 0)
                Dim id = SyntheticIdRangeStart + (value Mod SyntheticIdRangeSize)
                Return id.ToString().PadLeft(8, "0"c)
            End Using
        End Function

        ''' <summary>Capitalizes the first letter of each word and removes separators.</summary>
        Private Function TitleCaseNoSpaces(raw As String) As String
            Dim words = raw.Split({" "c, "_"c, "-"c}, StringSplitOptions.RemoveEmptyEntries)
            Dim sb As New StringBuilder()
            For Each w In words
                sb.Append(Char.ToUpperInvariant(w(0)))
                If w.Length > 1 Then sb.Append(w.Substring(1))
            Next
            Return sb.ToString()
        End Function

        ''' <summary>Removes spaces only, leaving the rest of the text (including casing) untouched.</summary>
        Private Function RemoveSpaces(raw As String) As String
            Return raw.Replace(" ", "")
        End Function

        ''' <summary>
        ''' Produces a DIM-safe name: accents are folded to their base letter
        ''' (é -> e, à -> a), then only ASCII letters and digits are kept.
        ''' DIM's file name pattern only accepts [0-9A-Za-z] after the "_"
        ''' separator, so dots, ampersands, hyphens, etc. are dropped.
        ''' </summary>
        Private ReadOnly AccentMap As Dictionary(Of Char, String) = BuildAccentMap()

        Private Function BuildAccentMap() As Dictionary(Of Char, String)
            Dim map As New Dictionary(Of Char, String)

            Dim Fold = Sub(baseLetter As String, accented As String)
                           For Each ch In accented
                               map(ch) = baseLetter
                           Next
                       End Sub

            Fold("a", "àáâãäåāăą")
            Fold("c", "çćĉċč")
            Fold("d", "ďđ")
            Fold("e", "èéêëēĕėęě")
            Fold("g", "ĝğġģ")
            Fold("h", "ĥħ")
            Fold("i", "ìíîïĩīĭįı")
            Fold("j", "ĵ")
            Fold("k", "ķ")
            Fold("l", "ĺļľŀł")
            Fold("n", "ñńņň")
            Fold("o", "òóôõöøōŏő")
            Fold("r", "ŕŗř")
            Fold("s", "śŝşš")
            Fold("t", "ţťŧ")
            Fold("u", "ùúûüũūŭůűų")
            Fold("w", "ŵ")
            Fold("y", "ýÿŷ")
            Fold("z", "źżž")
            Fold("A", "ÀÁÂÃÄÅĀĂĄ")
            Fold("C", "ÇĆĈĊČ")
            Fold("D", "ĎĐ")
            Fold("E", "ÈÉÊËĒĔĖĘĚ")
            Fold("G", "ĜĞĠĢ")
            Fold("H", "ĤĦ")
            Fold("I", "ÌÍÎÏĨĪĬĮİ")
            Fold("J", "Ĵ")
            Fold("K", "Ķ")
            Fold("L", "ĹĻĽĿŁ")
            Fold("N", "ÑŃŅŇ")
            Fold("O", "ÒÓÔÕÖØŌŎŐ")
            Fold("R", "ŔŖŘ")
            Fold("S", "ŚŜŞŠ")
            Fold("T", "ŢŤŦ")
            Fold("U", "ÙÚÛÜŨŪŬŮŰŲ")
            Fold("W", "Ŵ")
            Fold("Y", "ÝŶŸ")
            Fold("Z", "ŹŻŽ")

            ' Ligatures and letters that expand to two characters.
            map("æ"c) = "ae"
            map("Æ"c) = "AE"
            map("œ"c) = "oe"
            map("Œ"c) = "OE"
            map("ß"c) = "ss"

            Return map
        End Function

        ''' <summary>
        ''' Produces a DIM-safe name: accented letters are folded to their
        ''' base letter (é -> e, à -> a) using an explicit table, then only
        ''' ASCII letters and digits are kept. DIM's file name pattern only
        ''' accepts [0-9A-Za-z] after the "_" separator, so dots, ampersands,
        ''' hyphens, etc. are dropped. The table avoids string.Normalize,
        ''' which is unreliable with InvariantGlobalization enabled.
        ''' </summary>
        Private Function StripInvalidFileNameChars(raw As String) As String
            Dim sb As New StringBuilder()

            For Each c In raw
                Dim replacement As String = Nothing
                If AccentMap.TryGetValue(c, replacement) Then
                    sb.Append(replacement)
                ElseIf (c >= "a"c AndAlso c <= "z"c) OrElse
                       (c >= "A"c AndAlso c <= "Z"c) OrElse
                       (c >= "0"c AndAlso c <= "9"c) Then
                    sb.Append(c)
                End If
                ' Anything else (combining marks, dots, &, hyphens...) is dropped.
            Next

            Return sb.ToString()
        End Function

    End Module

End Namespace
