namespace NymrSignIn.Client.Register.Components;

public static class MedicalSections
{
    public sealed record MedicalGroup(string Title, string? Intro, IReadOnlyList<string> Items, string CssClass, string Icon);

    public static readonly IReadOnlyList<MedicalGroup> All =
    [
        new MedicalGroup(
            Title: "Medication and Fitness for Duty Today",
            Intro: null,
            Items:
            [
                "I am not taking any medication that may impair my ability to perform my duties safely.",
                "I am not under the influence of drugs or alcohol.",
                "I confirm that I am fit to resume my normal duties, including any safety-critical tasks associated with my role."
            ],
            CssClass: "medical-statement-fitness",
            Icon: "\u2705"),

        new MedicalGroup(
            Title: "Symptoms Affecting Safety",
            Intro: "I confirm that I am free from symptoms that could impair my performance, such as:",
            Items:
            [
                "Fatigue or excessive drowsiness",
                "Dizziness or blackouts",
                "Impaired vision or hearing",
                "Reduced mobility or coordination",
                "Effects of drugs or alcohol"
            ],
            CssClass: "medical-statement-symptoms",
            Icon: "\u26A0\uFE0F")
    ];
}
