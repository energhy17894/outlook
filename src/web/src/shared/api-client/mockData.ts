import type {
  ActionProposal,
  Evidence,
  MyWorkSummary,
  PhaseTransition,
  Project,
  TimelineEvent,
  WorkItem,
} from './types';

function evidence(partial: Partial<Evidence> & Pick<Evidence, 'id' | 'exactQuote' | 'author'>): Evidence {
  return {
    sourceType: 'mail',
    sourceTimestampUtc: '2026-09-15T08:12:00Z',
    outlookWebLink: 'https://outlook.office.com/mail/deeplink/read/AAMk...',
    verified: true,
    ...partial,
  };
}

export const mockProjects: Project[] = [
  {
    id: 'proj-1',
    name: 'Kuzey Bölge ERP Entegrasyonu',
    customerOrg: 'Kuzey Lojistik A.Ş.',
    currentPhase: 'test_uat',
    health: { score: 62, band: 'watch', drivers: ['Yanıt yok: 5 gün', '2 açık soru bekliyor'] },
  },
  {
    id: 'proj-2',
    name: 'Bulut Göç Projesi',
    customerOrg: 'Anka Sigorta',
    currentPhase: 'execution',
    health: { score: 88, band: 'good', drivers: ['Tüm taahhütler zamanında'] },
  },
  {
    id: 'proj-3',
    name: 'Şube Ağı Yenileme',
    customerOrg: 'Marmara Perakende',
    currentPhase: 'negotiation_contract',
    health: { score: 41, band: 'at_risk', drivers: ['Karar 9 gündür bekliyor', 'Sözleşme revizyonu gecikti'] },
  },
];

export const mockWorkItems: WorkItem[] = [
  {
    id: 'wi-1',
    kind: 'risk',
    title: 'UAT ortamı performans testinde zaman aşımı riski',
    projectId: 'proj-1',
    owner: 'Sen',
    status: 'open',
    confidence: 'high',
    reviewState: 'accepted',
    evidence: [
      evidence({
        id: 'ev-1',
        author: 'Ahmet Yılmaz',
        exactQuote: "Yük testinde 500 kullanıcı üzerinde zaman aşımları görüyoruz, bunu UAT'a taşımadan çözmemiz lazım.",
      }),
    ],
  },
  {
    id: 'wi-2',
    kind: 'decision',
    title: 'Ödeme entegrasyonu için üçüncü parti sağlayıcı seçildi',
    projectId: 'proj-2',
    owner: 'Proje Ekibi',
    status: 'accepted',
    confidence: 'high',
    reviewState: 'accepted',
    evidence: [
      evidence({
        id: 'ev-2',
        author: 'Elif Kaya',
        exactQuote: 'Değerlendirme sonrası PaySecure ile devam etme kararı aldık.',
      }),
    ],
  },
  {
    id: 'wi-3',
    kind: 'open_question',
    title: 'Sözleşme revizyonunda ceza maddesi netleşmedi',
    projectId: 'proj-3',
    owner: 'Sen',
    status: 'open',
    confidence: 'medium',
    reviewState: 'suggested',
    evidence: [
      evidence({
        id: 'ev-3',
        author: 'Mert Demir',
        exactQuote: 'Gecikme cezası oranını hukuk ekibiyle teyit edip döneceğiz.',
      }),
    ],
  },
  {
    id: 'wi-4',
    kind: 'task',
    title: 'Test senaryolarını müşteriyle birlikte gözden geçir',
    projectId: 'proj-1',
    owner: 'Sen',
    dueText: 'Cuma\'ya kadar',
    dueAtUtc: '2026-10-02T15:00:00Z',
    status: 'open',
    confidence: 'high',
    reviewState: 'accepted',
    evidence: [
      evidence({
        id: 'ev-4',
        author: 'Ahmet Yılmaz',
        exactQuote: "Cuma'ya kadar test senaryolarını birlikte gözden geçirelim mi?",
      }),
    ],
  },
  {
    id: 'wi-5',
    kind: 'commitment',
    title: 'Revize teklif gönderilecek',
    projectId: 'proj-3',
    owner: 'Sen',
    counterparty: 'Marmara Perakende',
    dueText: 'ay sonuna kadar',
    status: 'at_risk',
    confidence: 'medium',
    reviewState: 'suggested',
    evidence: [
      evidence({
        id: 'ev-5',
        author: 'Sen',
        exactQuote: 'Ay sonuna kadar revize teklifi tarafınıza ileteceğim.',
      }),
    ],
  },
  {
    id: 'wi-6',
    kind: 'request',
    title: 'Sunucu erişim bilgileri bekleniyor',
    projectId: 'proj-2',
    owner: 'Sen',
    counterparty: 'Anka Sigorta BT',
    status: 'open',
    confidence: 'high',
    reviewState: 'accepted',
    evidence: [
      evidence({
        id: 'ev-6',
        author: 'Anka Sigorta BT',
        exactQuote: 'Erişim bilgilerini yarın sabah paylaşacağız.',
      }),
    ],
  },
];

export const mockMyWork: MyWorkSummary = {
  myTasks: mockWorkItems.filter((w) => w.owner === 'Sen' && ['task', 'commitment'].includes(w.kind)),
  waitingOn: mockWorkItems.filter((w) => w.kind === 'request' || w.kind === 'open_question'),
};

export const mockActionProposals: ActionProposal[] = [
  {
    id: 'ap-1',
    kind: 'reply_draft',
    title: 'Ahmet Yılmaz\'a yanıt taslağı',
    summary: 'UAT performans riskine ilişkin bir sonraki adımı özetleyen yanıt taslağı.',
    payloadPreview:
      'Merhaba Ahmet, yük testi bulgularını aldık. Ekip Perşembe gününe kadar bir düzeltme planı paylaşacak...',
    rationale: 'Konuyla ilgili risk kaydı açıldı ve karşı taraf yanıt bekliyor (5 gündür sessizlik).',
    evidence: [
      evidence({
        id: 'ev-1',
        author: 'Ahmet Yılmaz',
        exactQuote: "Yük testinde 500 kullanıcı üzerinde zaman aşımları görüyoruz, bunu UAT'a taşımadan çözmemiz lazım.",
      }),
    ],
    riskFlags: [],
    status: 'pending_approval',
    projectId: 'proj-1',
  },
  {
    id: 'ap-2',
    kind: 'task',
    title: 'Görev oluştur: Sözleşme ceza maddesini hukukla teyit et',
    summary: 'Açık sorudan türetilen görev, sahibi hukuk ekibi olarak öneriliyor.',
    payloadPreview: 'Görev: Ceza maddesi oranını hukuk ekibiyle teyit et — Sahip: Sen — Termin: belirsiz',
    rationale: 'Açık soru 9 gündür kapanmadı ve proje sağlık göstergesinde "at_risk" olarak işaretli.',
    evidence: [
      evidence({
        id: 'ev-3',
        author: 'Mert Demir',
        exactQuote: 'Gecikme cezası oranını hukuk ekibiyle teyit edip döneceğiz.',
      }),
    ],
    riskFlags: ['Düşük güven: termin tarihi çıkarılamadı'],
    status: 'pending_approval',
    projectId: 'proj-3',
  },
  {
    id: 'ap-3',
    kind: 'calendar_hold',
    title: 'Katılımcısız toplantı bloğu: UAT düzeltme planı sunumu',
    summary: 'Takvimde geçici (tentative) bir blok önerilir; davet gönderilmez.',
    payloadPreview: 'Perşembe 14:00–15:00, "UAT düzeltme planı" — showAs: tentative, katılımcı yok',
    rationale: 'Karşı taraf Perşembe gününe kadar bir plan bekliyor.',
    evidence: [
      evidence({
        id: 'ev-1',
        author: 'Ahmet Yılmaz',
        exactQuote: "Yük testinde 500 kullanıcı üzerinde zaman aşımları görüyoruz, bunu UAT'a taşımadan çözmemiz lazım.",
      }),
    ],
    riskFlags: [],
    status: 'pending_approval',
    projectId: 'proj-1',
  },
];

export const mockTimelineEvents: TimelineEvent[] = [
  {
    id: 'evt-1',
    projectId: 'proj-1',
    activity: 'KickoffHeld',
    timestampUtc: '2026-06-02T09:00:00Z',
    actor: 'Proje Ekibi',
  },
  {
    id: 'evt-2',
    projectId: 'proj-1',
    activity: 'DesignDocShared',
    timestampUtc: '2026-07-10T11:30:00Z',
    actor: 'Ahmet Yılmaz',
  },
  {
    id: 'evt-3',
    projectId: 'proj-1',
    activity: 'UATStarted',
    timestampUtc: '2026-09-01T08:00:00Z',
    actor: 'Proje Ekibi',
  },
  {
    id: 'evt-4',
    projectId: 'proj-1',
    activity: 'RiskRaised',
    timestampUtc: '2026-09-15T08:12:00Z',
    actor: 'Ahmet Yılmaz',
    evidence: evidence({
      id: 'ev-1',
      author: 'Ahmet Yılmaz',
      exactQuote: "Yük testinde 500 kullanıcı üzerinde zaman aşımları görüyoruz, bunu UAT'a taşımadan çözmemiz lazım.",
    }),
  },
];

export const mockPhaseTransitions: PhaseTransition[] = [
  {
    id: 'pt-1',
    projectId: 'proj-1',
    fromPhase: 'execution',
    toPhase: 'test_uat',
    atUtc: '2026-09-01T08:00:00Z',
    confirmed: true,
    evidenceIds: ['ev-1'],
  },
];
