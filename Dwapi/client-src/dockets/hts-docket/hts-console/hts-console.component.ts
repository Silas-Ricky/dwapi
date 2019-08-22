import {Component, Input, OnChanges, OnDestroy, OnInit, SimpleChange} from '@angular/core';
import {EmrSystem} from '../../../settings/model/emr-system';
import {HubConnection, HubConnectionBuilder, LogLevel} from '@aspnet/signalr';
import {ConfirmationService, Message} from 'primeng/api';
import {HtsService} from '../../services/hts.service';
import {RegistryConfigService} from '../../../settings/services/registry-config.service';
import {HtsSenderService} from '../../services/hts-sender.service';
import {Subscription} from 'rxjs/Subscription';
import {Extract} from '../../../settings/model/extract';
import {DwhExtract} from '../../../settings/model/dwh-extract';
import {ExtractEvent} from '../../../settings/model/extract-event';
import {SendEvent} from '../../../settings/model/send-event';
import {SendPackage} from '../../../settings/model/send-package';
import {ExtractDatabaseProtocol} from '../../../settings/model/extract-protocol';
import {LoadFromEmrCommand} from '../../../settings/model/load-from-emr-command';
import {ExtractProfile} from '../../ndwh-docket/model/extract-profile';
import {CentralRegistry} from '../../../settings/model/central-registry';
import {SendResponse} from '../../../settings/model/send-response';
import {EmrConfigService} from '../../../settings/services/emr-config.service';
import {LoadExtracts} from '../../../settings/model/load-extracts';
import {LoadHtsExtracts} from '../../../settings/model/load-hts-extracts';
import {LoadHtsFromEmrCommand} from '../../../settings/model/load-hts-from-emr-command';
import {environment} from '../../../environments/environment';
import {el} from '@angular/platform-browser/testing/src/browser_util';

@Component({
    selector: 'liveapp-hts-console',
    templateUrl: './hts-console.component.html',
    styleUrls: ['./hts-console.component.scss']
})
export class HtsConsoleComponent implements OnInit, OnDestroy, OnChanges {
    @Input() emr: EmrSystem;
    @Input() emrVer: string;
    private _hubConnection: HubConnection | undefined;
    private _sendhubConnection: HubConnection | undefined;
    public async: any;

    public emrName: string;
    public emrVersion: string;

    private _confirmationService: ConfirmationService;
    private _htsService: HtsService;
    private _registryConfigService: RegistryConfigService;
    private _htsSenderService: HtsSenderService;

    public load$: Subscription;
    public loadRegistry$: Subscription;
    public send$: Subscription;
    public getStatus$: Subscription;
    public sendManifest$: Subscription;

    public loadingData: boolean;
    public extracts: Extract[] = [];
    public currentExtract: Extract;
    private dwhExtract: DwhExtract;
    private dwhExtracts: DwhExtract[] = [];
    private extractEvent: ExtractEvent;
    public sendEvent: SendEvent = {};
    public sendEventPartners: SendEvent = {};
    public sendEventLinkage: SendEvent = {};
    public recordCount: number;

    public canLoadFromEmr: boolean;
    public canSend: boolean;
    public canSendPatients: boolean = false;
    public manifestPackage: SendPackage;
    public patientPackage: SendPackage;
    public sending: boolean = false;
    public sendingManifest: boolean = false;

    public errorMessage: Message[];
    public otherMessage: Message[];
    public notifications: Message[];
    private _extractDbProtocol: ExtractDatabaseProtocol;
    private _extractDbProtocols: ExtractDatabaseProtocol[];
    private extractLoadCommand: LoadHtsFromEmrCommand;
    private loadExtractsCommand: LoadHtsExtracts;
    private extractClient: ExtractProfile;
    private extractClientLinkage: ExtractProfile;
    private extractClientPartner: ExtractProfile;

    private extractClients: ExtractProfile;
    private extractClientTests: ExtractProfile;
    private extractClientsLinkage: ExtractProfile;
    private extractTestKits: ExtractProfile;
    private extractClientTracing: ExtractProfile;
    private extractPartnerTracing: ExtractProfile;
    private extractPartnerNotificationServices: ExtractProfile;

    private extractProfile: ExtractProfile;
    private extractProfiles: ExtractProfile[] = [];
    public centralRegistry: CentralRegistry;
    public sendResponse: SendResponse;
    public getEmr$: Subscription;

    public sendStage = 2;
    extractSent = [];

    public constructor(
        confirmationService: ConfirmationService,
        emrConfigService: HtsService,
        registryConfigService: RegistryConfigService,
        psmartSenderService: HtsSenderService,
        private emrService: EmrConfigService
    ) {
        this._confirmationService = confirmationService;
        this._htsService = emrConfigService;
        this._registryConfigService = registryConfigService;
        this._htsSenderService = psmartSenderService;
    }

    public ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
        this.loadData();
    }

    public ngOnInit() {
        this.loadRegisrty();
        this.liveOnInit();
        this.loadData();
    }

    public loadData(): void {
        this.canLoadFromEmr = this.canSend = false;

        if (this.emr) {
            this.canLoadFromEmr = true;
            this.loadingData = true;
            this.recordCount = 0;
            this.extracts = this.emr.extracts.filter(
                x => x.docketId === 'HTS'
            );
            this.updateEvent();
            this.emrName = this.emr.name;
            this.emrVersion = `(Ver. ${this.emr.version})`;
        }
        if (this.centralRegistry) {
            this.canSend = true;
        }
    }

    public loadFromEmr(): void {
        this.errorMessage = [];
        this.load$ = this._htsService
            .extractAll(this.generateExtractLoadCommand(this.emr))
            .subscribe(
                p => {
                    // this.isVerfied = p;
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({
                        severity: 'error',
                        summary: 'Error verifying ',
                        detail: <any>e
                    });
                },
                () => {
                    this.errorMessage.push({
                        severity: 'success',
                        summary: 'load was successful '
                    });
                    this.updateEvent();
                }
            );
    }

    public loadRegisrty(): void {
        this.errorMessage = [];
        this.loadRegistry$ = this._registryConfigService.get('HTS').subscribe(
            p => {
                this.centralRegistry = p;
            },
            e => {
                this.errorMessage = [];
                this.errorMessage.push({
                    severity: 'error',
                    summary: 'Error loading regisrty ',
                    detail: <any>e
                });
            },
            () => {
            }
        );
    }

    public updateEvent(): void {
        this.extracts.forEach(extract => {
            this.getStatus$ = this._htsService
                .getStatus(extract.id)
                .subscribe(
                    p => {
                        extract.extractEvent = p;
                        if (extract.extractEvent) {
                            this.canSend = extract.extractEvent.queued > 0;
                        }
                    },
                    e => {
                        this.errorMessage = [];
                        this.errorMessage.push({
                            severity: 'error',
                            summary: 'Error loading status ',
                            detail: <any>e
                        });
                    },
                    () => {
                    }
                );
        });
    }


    public send(): void {
        this.sendingManifest = true;
        this.errorMessage = [];
        this.notifications = [];
        this.canSendPatients = false;
        this.manifestPackage = this.getSendManifestPackage();
        this.sendManifest$ = this._htsSenderService.sendManifest(this.manifestPackage)
            .subscribe(
                p => {
                    this.canSendPatients = true;
                    this.sendingManifest = false;
                    this.updateEvent();
                    this.sendClientsExtract();
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({severity: 'error', summary: 'Error sending ', detail: <any>e});
                },
                () => {
                    this.notifications.push({severity: 'success', summary: 'Manifest sent'});

                }
            );
    }

    public sendClientsExtract(): void {
        this.sendEvent = { sentProgress: 0 };
        this.sending = true;
        this.errorMessage = [];
        this.patientPackage = this.getClientsExtractPackage();
        this.send$ = this._htsSenderService.sendClientsExtracts(this.patientPackage)
            .subscribe(
                p => {
                    // this.sendResponse = p;
                    this.updateEvent();
                    this.sendClientTestsExtracts();
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({ severity: 'error', summary: 'Error sending client', detail: <any>e });
                },
                () => {
                    // this.errorMessage.push({severity: 'success', summary: 'sent Clients successfully '});
                }
            );
    }

    public sendClientTestsExtracts(): void {
        this.sendEvent = { sentProgress: 0 };
        this.sending = true;
        this.errorMessage = [];
        this.patientPackage = this.getClientTestsExtractPackage();
        this.send$ = this._htsSenderService.sendClientTestsExtracts(this.patientPackage)
            .subscribe(
                p => {
                    // this.sendResponse = p;
                    this.updateEvent();
                    this.sendTestKitsExtracts();
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({ severity: 'error', summary: 'Error sending client tests', detail: <any>e });
                },
                () => {
                    // this.errorMessage.push({severity: 'success', summary: 'sent Clients successfully '});
                }
            );
    }

    public sendTestKitsExtracts(): void {
        this.sendEvent = { sentProgress: 0 };
        this.sending = true;
        this.errorMessage = [];
        this.patientPackage = this.getTestKitsExtractPackage();
        this.send$ = this._htsSenderService.sendTestKitsExtracts(this.patientPackage)
            .subscribe(
                p => {
                    // this.sendResponse = p;
                    this.updateEvent();
                    this.sendClientTracingExtracts();
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({ severity: 'error', summary: 'Error sending test kits', detail: <any>e });
                },
                () => {
                    // this.errorMessage.push({severity: 'success', summary: 'sent Clients successfully '});
                }
            );
    }

    public sendClientTracingExtracts(): void {
        this.sendEvent = { sentProgress: 0 };
        this.sending = true;
        this.errorMessage = [];
        this.patientPackage = this.getClientTracingExtractPackage();
        this.send$ = this._htsSenderService.sendClientTracingExtracts(this.patientPackage)
            .subscribe(
                p => {
                    // this.sendResponse = p;
                    this.updateEvent();
                    this.sendPartnerTracingExtracts();
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({ severity: 'error', summary: 'Error sending client tracing', detail: <any>e });
                },
                () => {
                    // this.errorMessage.push({severity: 'success', summary: 'sent Clients successfully '});
                }
            );
    }

    public sendPartnerTracingExtracts(): void {
        this.sendEvent = { sentProgress: 0 };
        this.sending = true;
        this.errorMessage = [];
        this.patientPackage = this.getPartnerTracingExtractPackage();
        this.send$ = this._htsSenderService.sendPartnerTracingExtracts(this.patientPackage)
            .subscribe(
                p => {
                    // this.sendResponse = p;
                    this.updateEvent();
                    this.sendPartnerNotificationServicesExtracts();
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({ severity: 'error', summary: 'Error sending partner tracing', detail: <any>e });
                },
                () => {
                    // this.errorMessage.push({severity: 'success', summary: 'sent Clients successfully '});
                }
            );
    }

    public sendPartnerNotificationServicesExtracts(): void {
        this.sendEvent = { sentProgress: 0 };
        this.sending = true;
        this.errorMessage = [];
        this.patientPackage = this.getPartnerNotificationServicesExtractPackage();
        this.send$ = this._htsSenderService.sendPartnerNotificationServicesExtracts(this.patientPackage)
            .subscribe(
                p => {
                    // this.sendResponse = p;
                    this.updateEvent();
                    this.sendClientLinkageExtracts();
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({ severity: 'error', summary: 'Error sending partner notification service', detail: <any>e });
                },
                () => {
                    // this.errorMessage.push({severity: 'success', summary: 'sent Clients successfully '});
                }
            );
    }

    public sendClientLinkageExtracts(): void {
        this.sendEvent = { sentProgress: 0 };
        this.sending = true;
        this.errorMessage = [];
        this.patientPackage = this.getClientsLinkageExtractPackage();
        this.send$ = this._htsSenderService.sendClientLinkageExtracts(this.patientPackage)
            .subscribe(
                p => {
                    // this.sendResponse = p;
                    this.updateEvent();
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({ severity: 'error', summary: 'Error sending client linkage', detail: <any>e });
                },
                () => {
                    // this.errorMessage.push({severity: 'success', summary: 'sent Clients successfully '});
                }
            );
    }

    public sendClientExtract(): void {
        this.sendEvent = {sentProgress: 0};
        this.sending = true;
        this.errorMessage = [];
        this.patientPackage = this.getClientExtractPackage();
        this.send$ = this._htsSenderService.sendClientExtracts(this.patientPackage)
            .subscribe(
                p => {
                    // this.sendResponse = p;
                    this.updateEvent();
                    this.sendClientLinkageExtract();
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({severity: 'error', summary: 'Error sending Clients', detail: <any>e});
                },
                () => {
                    // this.errorMessage.push({severity: 'success', summary: 'sent Clients successfully '});
                }
            );
    }

    public sendClientLinkageExtract(): void {
        this.sendStage = 3;
        this.sendEvent = {sentProgress: 0};
        this.sending = true;
        this.errorMessage = [];
        this.patientPackage = this.getClientLinkageExtractPackage();
        this.send$ = this._htsSenderService.sendClientLinkageExtracts(this.patientPackage)
            .subscribe(
                p => {
                    // this.sendResponse = p;
                    this.updateEvent();
                    this.sendClientPartnerExtract();
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({severity: 'error', summary: 'Error sending Linkages', detail: <any>e});
                },
                () => {
                    // this.errorMessage.push({severity: 'success', summary: 'sent Linkages successfully '});

                }
            );
    }

    public sendClientPartnerExtract(): void {
        this.sendStage = 4;
        this.sendEvent = {sentProgress: 0};
        this.sending = true;
        this.errorMessage = [];
        this.patientPackage = this.getClientPartnerExtractPackage();
        this.send$ = this._htsSenderService.sendClientPartnerExtracts(this.patientPackage)
            .subscribe(
                p => {
                    // this.sendResponse = p;
                    this.updateEvent();
                    // this.sending = false;
                },
                e => {
                    this.errorMessage = [];
                    this.errorMessage.push({severity: 'error', summary: 'Error sending Partners ', detail: <any>e});
                },
                () => {
                    // this.errorMessage.push({severity: 'success', summary: 'sent Partners successfully '});


                }
            );
    }

    private getSendManifestPackage(): SendPackage {
        return {
            destination: this.centralRegistry,
            extractId: this.extracts.find(x => x.name === 'HTSClientExtract').id
        };
    }

    private getClientExtractPackage(): SendPackage {
        return {
            destination: this.centralRegistry,
            extractId: this.extracts.find(x => x.name === 'HTSClientExtract').id,
            extractName: 'HTSClientExtract'
        };
    }

    private getClientLinkageExtractPackage(): SendPackage {
        return {
            destination: this.centralRegistry,
            extractId: this.extracts.find(x => x.name === 'HTSClientLinkageExtract').id,
            extractName: 'HTSClientLinkageExtract'
        };
    }

    private getClientPartnerExtractPackage(): SendPackage {
        return {
            destination: this.centralRegistry,
            extractId: this.extracts.find(x => x.name === 'HTSClientPartnerExtract').id,
            extractName: 'HTSClientPartnerExtract'
        };
    }

    private getClientsExtractPackage(): SendPackage {
        return {
            destination: this.centralRegistry,
            extractId: this.extracts.find(x => x.name === 'HtsClientsExtracts').id,
            extractName: 'HtsClientsExtracts'
        };
    }

    private getPartnerNotificationServicesExtractPackage(): SendPackage {
        return {
            destination: this.centralRegistry,
            extractId: this.extracts.find(x => x.name === 'HtsPartnerNotificationServicesExtracts').id,
            extractName: 'HtsPartnerNotificationServicesExtracts'
        };
    }

    private getPartnerTracingExtractPackage(): SendPackage {
        return {
            destination: this.centralRegistry,
            extractId: this.extracts.find(x => x.name === 'HtsPartnerTracingExtracts').id,
            extractName: 'HtsPartnerTracingExtracts'
        };
    }

    private getClientTracingExtractPackage(): SendPackage {
        return {
            destination: this.centralRegistry,
            extractId: this.extracts.find(x => x.name === 'HtsClientTracingExtracts').id,
            extractName: 'HtsClientTracingExtracts'
        };
    }

    private getTestKitsExtractPackage(): SendPackage {
        return {
            destination: this.centralRegistry,
            extractId: this.extracts.find(x => x.name === 'HtsTestKitsExtracts').id,
            extractName: 'HtsTestKitsExtracts'
        };
    }

    private getClientsLinkageExtractPackage(): SendPackage {
        return {
            destination: this.centralRegistry,
            extractId: this.extracts.find(x => x.name === 'HtsClientsLinkageExtracts').id,
            extractName: 'HtsClientsLinkageExtracts'
        };
    }

    private getClientTestsExtractPackage(): SendPackage {
        return {
            destination: this.centralRegistry,
            extractId: this.extracts.find(x => x.name === 'HtsClientTestsExtracts').id,
            extractName: 'HtsClientTestsExtracts'
        };
    }

    private liveOnInit() {
        this._hubConnection = new HubConnectionBuilder()
            .withUrl(
                `http://${document.location.hostname}:${environment.port}/HtsActivity`
            )
            .configureLogging(LogLevel.Trace)
            .build();

        this._hubConnection.start().catch(err => console.error(err.toString()));

        this._hubConnection.on('ShowHtsProgress', (extractActivityNotification: any) => {
            this.currentExtract = {};
            this.currentExtract = this.extracts.find(
                x => x.name === extractActivityNotification.extract
            );
            if (this.currentExtract) {
                this.extractEvent = {
                    lastStatus: `${extractActivityNotification.status}`,
                    found: extractActivityNotification.found,
                    loaded: extractActivityNotification.loaded,
                    rejected: extractActivityNotification.rejected,
                    queued: extractActivityNotification.queued,
                    sent: extractActivityNotification.sent
                };
                this.currentExtract.extractEvent = {};
                this.currentExtract.extractEvent = this.extractEvent;
                const newWithoutPatientExtract = this.extracts.filter(
                    x => x.name !== extractActivityNotification.extract
                );
                this.extracts = [
                    ...newWithoutPatientExtract,
                    this.currentExtract
                ];
            }
        });

        this._hubConnection.on('ShowHtsSendProgress', (dwhProgress: any) => {
            this.sendEvent = {
                sentProgress: dwhProgress.progress
            };
            this.canLoadFromEmr = this.canSend = !this.sending;
        });

        this._hubConnection.on('ShowHtsSendProgressDone', (extractName: string) => {
            this.extractSent.push(extractName);
            if (this.extractSent.length === 3) {
                this.errorMessage = [];
                this.errorMessage.push({severity: 'success', summary: 'sent successfully '});
                this.updateEvent();
                this.sending = false;
            } else {
                this.updateEvent();
            }
        });
    }

    private getExtractProtocols(
        currentEmr: EmrSystem
    ): ExtractDatabaseProtocol[] {
        this._extractDbProtocols = [];
        this.extracts.forEach(e => {
            e.emr = currentEmr.name;
            this._extractDbProtocols.push({
                extract: e,
                databaseProtocol: currentEmr.databaseProtocols[0]
            });
        });
        return this._extractDbProtocols;
    }

    private generateExtractLoadCommand(currentEmr: EmrSystem): LoadHtsExtracts {
        this.extractProfiles.push(this.generateExtractClients(currentEmr));
        this.extractProfiles.push(this.generateExtractClientTests(currentEmr));
        this.extractProfiles.push(this.generateExtractPartnerNotificationServices(currentEmr));
        this.extractProfiles.push(this.generateExtractTestKits(currentEmr));
        this.extractProfiles.push(this.generateExtractClientsLinkage(currentEmr));
        this.extractProfiles.push(this.generateExtractPartnerTracing(currentEmr));
        this.extractProfiles.push(this.generateExtractClientTracing(currentEmr));

        this.extractLoadCommand = {
            extracts: this.extractProfiles
        };

        this.loadExtractsCommand = {
            loadHtsFromEmrCommand: this.extractLoadCommand
        };
        return this.loadExtractsCommand;
    }

    private generateExtractClients(currentEmr: EmrSystem): ExtractProfile {
        const selectedProtocal = this.extracts.find(x => x.name === 'HtsClientExtract').databaseProtocolId;
        this.extractClients = {
            databaseProtocol: currentEmr.databaseProtocols.filter(x => x.id === selectedProtocal)[0],
            extract: this.extracts.find(x => x.name === 'HtsClientExtract')
        };
        return this.extractClients;
    }

    private generateExtractClientTests(currentEmr: EmrSystem): ExtractProfile {
        const selectedProtocal = this.extracts.find(x => x.name === 'HtsClientTestsExtract').databaseProtocolId;
        this.extractClientTests = {
            databaseProtocol: currentEmr.databaseProtocols.filter(x => x.id === selectedProtocal)[0],
            extract: this.extracts.find(x => x.name === 'HtsClientTestsExtract')
        };
        return this.extractClientTests;
    }

    private generateExtractPartnerNotificationServices(currentEmr: EmrSystem): ExtractProfile {
        const selectedProtocal = this.extracts.find(x => x.name === 'HtsPartnerNotificationServicesExtract').databaseProtocolId;
        this.extractPartnerNotificationServices = {
            databaseProtocol: currentEmr.databaseProtocols.filter(x => x.id === selectedProtocal)[0],
            extract: this.extracts.find(x => x.name === 'HtsPartnerNotificationServicesExtract')
        };
        return this.extractPartnerNotificationServices;
    }

    private generateExtractTestKits(currentEmr: EmrSystem): ExtractProfile {
        const selectedProtocal = this.extracts.find(x => x.name === 'HtsTestKitsExtract').databaseProtocolId;
        this.extractTestKits = {
            databaseProtocol: currentEmr.databaseProtocols.filter(x => x.id === selectedProtocal)[0],
            extract: this.extracts.find(x => x.name === 'HtsTestKitsExtract')
        };
        return this.extractTestKits;
    }

    private generateExtractClientsLinkage(currentEmr: EmrSystem): ExtractProfile {
        const selectedProtocal = this.extracts.find(x => x.name === 'HtsClientLinkageExtract').databaseProtocolId;
        this.extractClientsLinkage = {
            databaseProtocol: currentEmr.databaseProtocols.filter(x => x.id === selectedProtocal)[0],
            extract: this.extracts.find(x => x.name === 'HtsClientLinkageExtract')
        };
        return this.extractClientsLinkage;
    }

    private generateExtractPartnerTracing(currentEmr: EmrSystem): ExtractProfile {
        const selectedProtocal = this.extracts.find(x => x.name === 'HtsPartnerTracingExtract').databaseProtocolId;
        this.extractPartnerTracing = {
            databaseProtocol: currentEmr.databaseProtocols.filter(x => x.id === selectedProtocal)[0],
            extract: this.extracts.find(x => x.name === 'HtsPartnerTracingExtract')
        };
        return this.extractPartnerTracing;
    }

    private generateExtractClientTracing(currentEmr: EmrSystem): ExtractProfile {
        const selectedProtocal = this.extracts.find(x => x.name === 'HtsClientTracingExtract').databaseProtocolId;
        this.extractClientTracing = {
            databaseProtocol: currentEmr.databaseProtocols.filter(x => x.id === selectedProtocal)[0],
            extract: this.extracts.find(x => x.name === 'HtsClientTracingExtract')
        };
        return this.extractClientTracing;
    }

    /*private generateExtractClient(currentEmr: EmrSystem): ExtractProfile {
        const selectedProtocal = this.extracts.find(x => x.name === 'HTSClientExtract').databaseProtocolId;
        this.extractClient = {
            databaseProtocol: currentEmr.databaseProtocols.filter(x => x.id === selectedProtocal)[0],
            extract: this.extracts.find(x => x.name === 'HTSClientExtract')
        };
        return this.extractClient;
    }

    private generateExtractClientLinkage(currentEmr: EmrSystem): ExtractProfile {
        const selectedProtocal = this.extracts.find(x => x.name === 'HTSClientLinkageExtract').databaseProtocolId;
        this.extractClientLinkage = {
            databaseProtocol: currentEmr.databaseProtocols.filter(x => x.id === selectedProtocal)[0],
            extract: this.extracts.find(x => x.name === 'HTSClientLinkageExtract')
        };
        return this.extractClientLinkage;
    }

    private generateExtractClientPartner(currentEmr: EmrSystem): ExtractProfile {
        const selectedProtocal = this.extracts.find(x => x.name === 'HTSClientPartnerExtract').databaseProtocolId;
        this.extractClientPartner = {
            databaseProtocol: currentEmr.databaseProtocols.filter(x => x.id === selectedProtocal)[0],
            extract: this.extracts.find(x => x.name === 'HTSClientPartnerExtract')
        };
        return this.extractClientPartner;
    }*/

    private getSendPackage(docketId: string): SendPackage {
        return {
            extractId: this.extracts[0].id,
            destination: this.centralRegistry,
            docket: docketId,
            endpoint: ''
        };
    }

    public ngOnDestroy(): void {
        if (this.load$) {
            this.load$.unsubscribe();
        }
        if (this.loadRegistry$) {
            this.loadRegistry$.unsubscribe();
        }
        if (this.send$) {
            this.send$.unsubscribe();
        }
    }
}
